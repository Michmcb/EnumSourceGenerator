namespace EnumSourceGenerator;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;

[Generator]
public sealed class SourceGen : ISourceGenerator
{
	public const string EnumGenAttributeName = "EnumGen";
	public const string NameAttributeName = "Name";
	// TODO is there any way rather than this crap to detect our attribute reliably? I mean, they can always use a using statement to alias it and then we're screwed...
	private static readonly HashSet<string> EnumGenAttributeNames = [EnumGenAttributeName, EnumGenAttributeName + "Attribute", "EnumSourceGenerator." + EnumGenAttributeName, "EnumSourceGenerator." + EnumGenAttributeName + "Attribute"];
	private static readonly HashSet<string> NameAttributeNames = [NameAttributeName, NameAttributeName + "Attribute", "EnumSourceGenerator." + NameAttributeName, "EnumSourceGenerator." + NameAttributeName + "Attribute"];
	public void Initialize(GeneratorInitializationContext context)
	{
		// Calling this method for our attributes allows us to access the constructor arguments by using symbols in the execute method below
		context.RegisterForPostInitialization((i) => i.AddSource("EnumSourceGeneratorAttributes.g.cs", SourceText.From("namespace EnumSourceGenerator\n" +
"{\n" +
"\tusing System;\n" +

"\t/// <summary>\n" +
"\t/// Decorating an enum with this attribute will cause various methods to be generated for it.\n" +
"\t/// </summary>\n" +
"\t[AttributeUsage(AttributeTargets.Enum)]\n" +
"\tpublic sealed class " + EnumGenAttributeName + "Attribute : Attribute\n" +
"\t{\n" +
"\t\tpublic EnumGenAttribute(StringComparison comparer = StringComparison.Ordinal)\n" +
"\t\t{\n" +
"\t\t\tComparer = comparer;\n" +
"\t\t}\n" +
"\t\tpublic StringComparison Comparer { get; }\n" +
"\t}\n" +
"\t/// <summary>\n" +
"\t/// Decorating an enum member with this attribute will define its string representation.\n" +
"\t/// </summary>\n" +
"\t[AttributeUsage(AttributeTargets.Field)]\n" +
"\tpublic sealed class " + NameAttributeName + "Attribute : Attribute\n" +
"\t{\n" +
"\t\tpublic NameAttribute(string text)\n" +
"\t\t{\n" +
"\t\t\tText = text;\n" +
"\t\t}\n" +
"\t\tpublic string Text { get; }\n" +
"\t}\n" +
"}\n", Encoding.UTF8)));
		//#if DEBUG
		//		if (!System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Launch();
		//#endif
	}
	public void Execute(GeneratorExecutionContext context)
	{
		CancellationToken ct = context.CancellationToken;

		// TODO we need different attributes, one where you can decorate a static partial class and we generate methods on it for the specified enum, where the user does not control the enum in question. Should be the same names as the existing ones, but with "For" suffixed.

		IEnumerable<EnumDeclarationSyntax> targetEnums = context.Compilation.SyntaxTrees
			.SelectMany(x => x.GetRoot(ct).DescendantNodes())
			.OfType<EnumDeclarationSyntax>()
			.Where(x => x.AttributeLists.Any(al => al.Attributes.Any(a => EnumGenAttributeNames.Contains(a.Name.ToString()))));

		foreach (var targetEnum in targetEnums)
		{
			string enumName = targetEnum.Identifier.ToString();
			SemanticModel smTargetEnum = context.Compilation.GetSemanticModel(targetEnum.SyntaxTree);
			ISymbol? symTargetEnum = smTargetEnum.GetDeclaredSymbol(targetEnum);

			if (symTargetEnum == null)
			{
				context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(id: "ESG9999", title: "Failed to get symbol",
					messageFormat: "Could not get symbol for enum member {0}.",
					"category", DiagnosticSeverity.Error, isEnabledByDefault: true), Location.Create(targetEnum.SyntaxTree, targetEnum.Span), enumName));
				continue;
			}

			bool isFlags = symTargetEnum.GetAttributes().Any(x => x.ToString() == "System.FlagsAttribute");

			string? underlyingType = null;
			Func<object, string>? underlyingTypeAsString = null;
			if (symTargetEnum is INamedTypeSymbol ntsTargetEnum && ntsTargetEnum.EnumUnderlyingType != null)
			{
				underlyingType = ntsTargetEnum.EnumUnderlyingType.ToString();
				switch (underlyingType)
				{
					case "sbyte":
						underlyingTypeAsString = o => ((sbyte)o).ToString(CultureInfo.InvariantCulture);
						break;
					case "byte":
						underlyingTypeAsString = o => ((byte)o).ToString(CultureInfo.InvariantCulture);
						break;
					case "short":
						underlyingTypeAsString = o => ((short)o).ToString(CultureInfo.InvariantCulture);
						break;
					case "ushort":
						underlyingTypeAsString = o => ((ushort)o).ToString(CultureInfo.InvariantCulture);
						break;
					case "int":
						underlyingTypeAsString = o => ((int)o).ToString(CultureInfo.InvariantCulture);
						break;
					case "uint":
						underlyingTypeAsString = o => ((uint)o).ToString(CultureInfo.InvariantCulture);
						break;
					case "long":
						underlyingTypeAsString = o => ((long)o).ToString(CultureInfo.InvariantCulture);
						break;
					case "ulong":
						underlyingTypeAsString = o => ((ulong)o).ToString(CultureInfo.InvariantCulture);
						break;
					default:
						underlyingType = null;
						break;
				}
			}
			if (underlyingType == null || underlyingTypeAsString == null)
			{
				context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(id: "ESG0002", title: "Enum has invalid underlying type",
					messageFormat: "Enum {0} has an invalid underlying type; must be one of sbyte, byte, short, ushort, int, uint, long, ulong.",
					"category", DiagnosticSeverity.Error, isEnabledByDefault: true), Location.Create(targetEnum.SyntaxTree, targetEnum.Span), enumName));
				continue;
			}
			if (symTargetEnum.ContainingNamespace == null)
			{
				context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(id: "ESG0001", title: "Enum missing namespace",
					messageFormat: "Enum {0} must be declared in a namespace.",
					"category", DiagnosticSeverity.Error, isEnabledByDefault: true), Location.Create(targetEnum.SyntaxTree, targetEnum.Span), enumName));
				continue;
			}

			string enumNamespace = symTargetEnum.ContainingNamespace.ToString();
			StringComparison comparison = StringComparison.Ordinal;

			{
				AttributeData? attrib = symTargetEnum.GetAttributes()
					.Where(x => x.AttributeClass != null && NameAttributeNames.Contains(x.AttributeClass.Name))
					.FirstOrDefault();

				if (attrib != null)
				{
					ImmutableArray<TypedConstant> attribArgs = attrib.ConstructorArguments;
					if (attribArgs.Length == 1)
					{
						object? o = attribArgs[0].Value;
						if (o is not null)
						{
							comparison = (StringComparison)o;
						}
					}
				}
			}

			// What we want to do is this:
			// Get every single enum member, and stuff it into a DTO, along with its string representation
			// Also get every attribute that decorates the enum members
			// Once we have done that, we can check for missing styles
			// If there's a missing so-called "style", then we have to issue a warning, but we can just fall back to the default string representation otherwise.
			// For every so-called style of string representation, we need a distinct ToStr and TryParse method.
			// As well as a NameValues method and dictionary to support the TryParse method

			List<EnumData> enumData = [];

			foreach (var enumMember in targetEnum.DescendantNodes()
				.OfType<EnumMemberDeclarationSyntax>())
			{
				ISymbol? symEnumMember = smTargetEnum.GetDeclaredSymbol(enumMember);
				if (symEnumMember == null || symEnumMember is not IFieldSymbol fsEnumMember)
				{
					context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(id: "ESG9999", title: "Failed to get symbol",
						messageFormat: "Could not get symbol for enum member {0}.",
						"category", DiagnosticSeverity.Error, isEnabledByDefault: true), Location.Create(enumMember.SyntaxTree, enumMember.Span), enumMember.Identifier.ToString()));
					continue;
				}
				if (fsEnumMember.ConstantValue == null)
				{
					context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(id: "ESG0003", title: "Failed to get value",
						messageFormat: "Could not get underlying value for enum member {0}.",
						"category", DiagnosticSeverity.Error, isEnabledByDefault: true), Location.Create(enumMember.SyntaxTree, enumMember.Span), enumMember.Identifier.ToString()));
					continue;
				}
				var attribs = symEnumMember.GetAttributes();
				AttributeData? attrib = attribs
					.Where(x => x.AttributeClass != null && NameAttributeNames.Contains(x.AttributeClass.Name))
					.FirstOrDefault();
				string? customId = null;

				if (attrib != null)
				{
					ImmutableArray<TypedConstant> attribArgs = attrib.ConstructorArguments;
					if (attribArgs.Length == 1)
					{
						object? o = attribArgs[0].Value;
						if (!(o is null) && o is string str)
						{
							customId = str.Replace("\"", "\\\"");
						}
					}
				}

				string id = enumMember.Identifier.ToString();
				string stringValue = underlyingTypeAsString(fsEnumMember.ConstantValue);
				enumData.Add(new EnumData(customId ?? id, id, customId == null, stringValue));
			}

			StringBuilder sbEnum = new("#nullable enable\nnamespace ");
			sbEnum.Append(enumNamespace);
			sbEnum.Append("\n{\n");

			StringBuilder sbEnum_ToStr = new();
			sbEnum_ToStr.Append("\t\t/// <summary>\n");
			sbEnum_ToStr.Append("\t\t/// Returns a string representation of this enumeration value.\n");
			sbEnum_ToStr.Append("\t\t/// </summary>\n");
			sbEnum_ToStr.Append("\t\tpublic static string ToStr(this ");
			sbEnum_ToStr.Append(enumName);
			sbEnum_ToStr.Append(" value)\n");
			sbEnum_ToStr.Append("\t\t{\n");
			sbEnum_ToStr.Append("\t\t\tswitch (value)\n");
			sbEnum_ToStr.Append("\t\t\t{\n");

			StringBuilder sbEnum_IsDefined = new();
			sbEnum_IsDefined.Append("\t\t/// <summary>\n");
			sbEnum_IsDefined.Append("\t\t/// Returns true if this enumeration value is defined, false otherwise. Returns false if passing a combination of flags, and that combination of flags is not explicitly defined.\n");
			sbEnum_IsDefined.Append("\t\t/// </summary>\n");
			sbEnum_IsDefined.Append("\t\tpublic static bool IsDefined(this ");
			sbEnum_IsDefined.Append(enumName);
			sbEnum_IsDefined.Append(" value)\n");
			sbEnum_IsDefined.Append("\t\t{\n");
			sbEnum_IsDefined.Append("\t\t\tswitch (value)\n");
			sbEnum_IsDefined.Append("\t\t\t{\n");

			string enumClassName = "Enum" + enumName;
			sbEnum.Append("\tpublic static partial class ");
			sbEnum.Append(enumClassName);
			sbEnum.Append("\n\t{\n");

			StringBuilder sbEnum_NamesToValues = new("\t\tprivate static readonly System.Collections.Generic.Dictionary<string, ");
			sbEnum_NamesToValues.Append(enumName);
			sbEnum_NamesToValues.Append("> namesToValues = new System.Collections.Generic.Dictionary<string, ");
			sbEnum_NamesToValues.Append(enumName);
			sbEnum_NamesToValues.Append(">(");
			switch (comparison)
			{
				case StringComparison.CurrentCulture:
					sbEnum_NamesToValues.Append("System.StringComparer.CurrentCulture");
					break;
				case StringComparison.CurrentCultureIgnoreCase:
					sbEnum_NamesToValues.Append("System.StringComparer.CurrentCultureIgnoreCase");
					break;
				case StringComparison.InvariantCulture:
					sbEnum_NamesToValues.Append("System.StringComparer.InvariantCulture");
					break;
				case StringComparison.InvariantCultureIgnoreCase:
					sbEnum_NamesToValues.Append("System.StringComparer.InvariantCultureIgnoreCase");
					break;
				default:
				case StringComparison.Ordinal:
					sbEnum_NamesToValues.Append("System.StringComparer.Ordinal");
					break;
				case StringComparison.OrdinalIgnoreCase:
					sbEnum_NamesToValues.Append("System.StringComparer.OrdinalIgnoreCase");
					break;
			}
			sbEnum_NamesToValues.Append(")\n");
			sbEnum_NamesToValues.Append("\t\t{\n");

			StringBuilder sbEnum_UnderlyingValues = new("\t\tprivate static readonly ");
			sbEnum_UnderlyingValues.Append(underlyingType);
			sbEnum_UnderlyingValues.Append("[] underlyingValues = new ");
			sbEnum_UnderlyingValues.Append(underlyingType);
			sbEnum_UnderlyingValues.Append("[] { ");

			StringBuilder sbEnum_Values = new("\t\tprivate static readonly ");
			sbEnum_Values.Append(enumName);
			sbEnum_Values.Append("[] values = new ");
			sbEnum_Values.Append(enumName);
			sbEnum_Values.Append("[] { ");

			StringBuilder sbEnum_Names = new("\t\tprivate static readonly string[] names = new string[] { ");

			foreach (EnumData enumDatum in enumData)
			{
				string fullEnumName = enumDatum.UsesIdentifierAsName
					? string.Concat("nameof(", enumName, ".", enumDatum.Identifier, ")")
					: string.Concat("\"", enumDatum.Name, "\"");
				sbEnum_ToStr.Append("\t\t\t\tcase ").Append(enumName).Append('.').Append(enumDatum.Identifier);
				sbEnum_ToStr.Append(": return ").Append(fullEnumName).Append(";\n");

				sbEnum_IsDefined.Append("\t\t\t\tcase ").Append(enumName).Append('.').Append(enumDatum.Identifier).Append(":\n");

				sbEnum_NamesToValues.Append("\t\t\t[").Append(fullEnumName).Append("] = ");
				sbEnum_NamesToValues.Append(enumName).Append('.').Append(enumDatum.Identifier).Append(",\n");

				sbEnum_Values.Append(enumName).Append('.').Append(enumDatum.Identifier).Append(", ");
				sbEnum_UnderlyingValues.Append(enumDatum.ValueAsString).Append(", ");
				sbEnum_Names.Append(fullEnumName).Append(", ");
			}

			sbEnum_Values.Append("};\n");
			sbEnum_UnderlyingValues.Append("};\n");
			sbEnum_Names.Append("};\n");
			sbEnum_NamesToValues.Append("\t\t};\n");

			if (isFlags)
			{
				sbEnum_ToStr.Append("\t\t\t\tdefault: return FlagsToStr(value, \"|\");\n");
			}
			else
			{
				sbEnum_ToStr.Append("\t\t\t\tdefault: return string.Empty;\n");
			}
			sbEnum_ToStr.Append("\t\t\t}\n");
			sbEnum_ToStr.Append("\t\t}\n");

			sbEnum_IsDefined.Append("\t\t\t\t\treturn true;\n");
			sbEnum_IsDefined.Append("\t\t\t\tdefault: return false;\n");
			sbEnum_IsDefined.Append("\t\t\t}\n");
			sbEnum_IsDefined.Append("\t\t}\n");

			sbEnum.Append(sbEnum_Values);
			sbEnum.Append(sbEnum_UnderlyingValues);
			sbEnum.Append(sbEnum_Names);
			sbEnum.Append(sbEnum_NamesToValues);
			sbEnum.Append(sbEnum_ToStr);
			sbEnum.Append(sbEnum_IsDefined);

			sbEnum.Append("\t\t/// <summary>\n");
			sbEnum.Append("\t\t/// Attempts to parse <paramref name=\"value\"/>, returning <see langword=\"true\"/> on success and <see langword=\"false\"/> on failure.\n");
			sbEnum.Append("\t\t/// </summary>\n");
			sbEnum.Append("\t\tpublic static bool TryParse(string? value, out ");
			sbEnum.Append(enumName);
			sbEnum.Append(" result)\n");
			sbEnum.Append("\t\t{\n");
			sbEnum.Append("\t\t\tif (value != null)\n");
			sbEnum.Append("\t\t\t{\n");
			sbEnum.Append("\t\t\t\treturn namesToValues.TryGetValue(value, out result);\n");
			sbEnum.Append("\t\t\t}\n");
			sbEnum.Append("\t\t\telse\n");
			sbEnum.Append("\t\t\t{\n");
			sbEnum.Append("\t\t\t\tresult = default;\n");
			sbEnum.Append("\t\t\t\treturn false;\n");
			sbEnum.Append("\t\t\t}\n");
			sbEnum.Append("\t\t}\n");

			sbEnum.Append("\t\t/// <summary>\n");
			sbEnum.Append("\t\t/// Attempts to parse <paramref name=\"value\"/>. Throws <see cref=\"System.ArgumentException\"/> on failure.\n");
			sbEnum.Append("\t\t/// </summary>\n");
			sbEnum.Append("\t\tpublic static ");
			sbEnum.Append(enumName);
			sbEnum.Append(" Parse(string? value)\n");
			sbEnum.Append("\t\t{\n");
			sbEnum.Append("\t\t\treturn TryParse(value, out var result) ? result : throw new System.ArgumentException(\"Unable to parse the provided value as \\\"");
			sbEnum.Append(enumName);
			sbEnum.Append("\\\". Value is: \" + value);\n");
			sbEnum.Append("\t\t}\n");

			if (isFlags)
			{
				// TODO we can optimize FlagsToStr better. If it's kept public we cannot make any assumptions but if it's make private, we can assume that it's not 0, and it's got >1 flag set.
				sbEnum.Append("\t\t/// <summary>\n");
				sbEnum.Append("\t\t/// Returns a string representing all set flags, delimited by |.\n");
				sbEnum.Append("\t\t/// </summary>\n");
				sbEnum.Append("\t\tpublic static string FlagsToStr(this ").Append(enumName).Append(" value, string delimiter)\n");
				sbEnum.Append("\t\t{\n");
				sbEnum.Append("\t\t\tif (value == default) return \"\";\n");
				sbEnum.Append("\t\t\tSystem.Text.StringBuilder s = new System.Text.StringBuilder();\n");
				sbEnum.Append("\t\t\tforeach (var f in EnumTestEnum.ValuesAsSpan)\n");
				sbEnum.Append("\t\t\t{\n");
				sbEnum.Append("\t\t\t\tif (f != 0 && (value & f) == f)\n");
				sbEnum.Append("\t\t\t\t{\n");
				sbEnum.Append("\t\t\t\t\ts.Append(f.ToStr()).Append(delimiter);\n");
				sbEnum.Append("\t\t\t\t}\n");
				sbEnum.Append("\t\t\t}\n");
				sbEnum.Append("\t\t\ts.Length -= delimiter.Length;\n");
				sbEnum.Append("\t\t\treturn s.ToString();\n");
				sbEnum.Append("\t\t}\n");

				sbEnum.Append("\t\t/// <summary>\n");
				sbEnum.Append("\t\t/// Returns <see langword=\"true\"/> if <paramref name=\"flag\"/> is set. Otherwise, returns <see langword=\"false\"/>.\n");
				sbEnum.Append("\t\t/// </summary>\n");
				sbEnum.Append("\t\tpublic static bool Flag(this ");
				sbEnum.Append(enumName);
				sbEnum.Append(" value, ");
				sbEnum.Append(enumName);
				sbEnum.Append(" flag)\n");
				sbEnum.Append("\t\t{\n");
				sbEnum.Append("\t\t\treturn (value & flag) == flag;\n");
				sbEnum.Append("\t\t}\n");
			}

			sbEnum.Append("\t\t/// <summary>\n");
			sbEnum.Append("\t\t/// The underlying type.\n");
			sbEnum.Append("\t\t/// </summary>\n");
			sbEnum.Append("\t\tpublic static System.Type UnderlyingType => typeof(");
			sbEnum.Append(underlyingType);
			sbEnum.Append(");\n");

			sbEnum.Append("\t\t/// <summary>\n");
			sbEnum.Append("\t\t/// Returns all the names and values, as tuples.\n");
			sbEnum.Append("\t\t/// </summary>\n");
			sbEnum.Append("\t\tpublic static System.Collections.Generic.IEnumerable<(string Name, ").Append(enumName).Append(" Value)> GetNameValues()\n");
			sbEnum.Append("\t\t{\n");
			sbEnum.Append("\t\t\tSystem.Diagnostics.Debug.Assert(names.Length == values.Length);\n");
			sbEnum.Append("\t\t\tfor(int i = 0; i < values.Length; ++i) { yield return (names[i], values[i]); }\n");
			sbEnum.Append("\t\t}\n");

			sbEnum.Append("\t\t/// <summary>\n");
			sbEnum.Append("\t\t/// Returns all the names and underlying values, as tuples.\n");
			sbEnum.Append("\t\t/// </summary>\n");
			sbEnum.Append("\t\tpublic static System.Collections.Generic.IEnumerable<(string Name, ").Append(underlyingType).Append(" Value)> GetNameUnderlyingValues()\n");
			sbEnum.Append("\t\t{\n");
			sbEnum.Append("\t\t\tSystem.Diagnostics.Debug.Assert(names.Length == values.Length);\n");
			sbEnum.Append("\t\t\tfor(int i = 0; i < values.Length; ++i) { yield return (names[i], (").Append(underlyingType).Append(")values[i]); }\n");
			sbEnum.Append("\t\t}\n");

			sbEnum.Append("\t\t/// <summary>\n");
			sbEnum.Append("\t\t/// A <see cref=\"System.ReadOnlySpan{string}\"/> containing all names.\n");
			sbEnum.Append("\t\t/// </summary>\n");
			sbEnum.Append("\t\tpublic static System.ReadOnlySpan<string> NamesAsSpan => new System.ReadOnlySpan<string>(names);\n");
			sbEnum.Append("\t\t/// <summary>\n");
			sbEnum.Append("\t\t/// A <see cref=\"System.ReadOnlyMemory{string}\"/> containing all names.\n");
			sbEnum.Append("\t\t/// </summary>\n");
			sbEnum.Append("\t\tpublic static System.ReadOnlyMemory<string> NamesAsMemory => new System.ReadOnlyMemory<string>(names);\n");

			sbEnum.Append("\t\t/// <summary>\n");
			sbEnum.Append("\t\t/// A <see cref=\"System.ReadOnlySpan{").Append(enumName).Append("}\"/> containing all values.\n");
			sbEnum.Append("\t\t/// </summary>\n");
			sbEnum.Append("\t\tpublic static System.ReadOnlySpan<").Append(enumName).Append("> ValuesAsSpan => new System.ReadOnlySpan<").Append(enumName).Append(">(values);\n");
			sbEnum.Append("\t\t/// <summary>\n");
			sbEnum.Append("\t\t/// A <see cref=\"System.ReadOnlyMemory{").Append(enumName).Append("}\"/> containing all values.\n");
			sbEnum.Append("\t\t/// </summary>\n");
			sbEnum.Append("\t\tpublic static System.ReadOnlyMemory<").Append(enumName).Append("> ValuesAsMemory => new System.ReadOnlyMemory<").Append(enumName).Append(">(values);\n");

			sbEnum.Append("\t\t/// <summary>\n");
			sbEnum.Append("\t\t/// A <see cref=\"System.ReadOnlySpan{").Append(underlyingType).Append("}\"/> containing all underlying values.\n");
			sbEnum.Append("\t\t/// </summary>\n");
			sbEnum.Append("\t\tpublic static System.ReadOnlySpan<").Append(underlyingType).Append("> UnderlyingValuesAsSpan => new System.ReadOnlySpan<").Append(underlyingType).Append(">(underlyingValues);\n");
			sbEnum.Append("\t\t/// <summary>\n");
			sbEnum.Append("\t\t/// A <see cref=\"System.ReadOnlyMemory{").Append(underlyingType).Append("}\"/> containing all underlying values.\n");
			sbEnum.Append("\t\t/// </summary>\n");
			sbEnum.Append("\t\tpublic static System.ReadOnlyMemory<").Append(underlyingType).Append("> UnderlyingValuesAsMemory => new System.ReadOnlyMemory<").Append(underlyingType).Append(">(underlyingValues);\n");

			sbEnum.Append("\t}\n");
			sbEnum.Append("}\n");
			sbEnum.Append("#nullable restore");

			string sourceEnum = sbEnum.ToString();
			context.AddSource(enumClassName + ".g.cs", SourceText.From(sourceEnum, Encoding.UTF8));
		}
	}
}