namespace EnumSourceGenerator;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

[Generator]
public sealed class SourceGen : ISourceGenerator
{
	public const string EnumGenAttributeName = "EnumGen";
	public const string EnumGenAttributeAttributeName = EnumGenAttributeName + "Attribute";
	public const string NameAttributeName = "Name";
	public const string NameAttributeAttributeName = NameAttributeName + "Attribute";
	public void Initialize(GeneratorInitializationContext context)
	{
		// Calling this method for our attributes allows us to access the constructor arguments by using symbols in the execute method below
		context.RegisterForPostInitialization((i) => i.AddSource("EnumSourceGeneratorAttributes.g.cs", SourceText.From("namespace EnumSourceGenerator\n" +
"{\n" +
"\tusing System;\n" +

"\t/// <summary>\n" +
"\t/// Decorating an enum with this attribute will cause ToStr() and TryParse() methods to be generated for it.\n" +
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
		
		// TODO we have to handle [Flags] as well...
		
		IEnumerable<EnumDeclarationSyntax> targetEnums = context.Compilation.SyntaxTrees
			.SelectMany(x => x.GetRoot(ct).DescendantNodes())
			.OfType<EnumDeclarationSyntax>()
			.Where(x => x.AttributeLists.Any(al => al.Attributes.Any(a =>
			{
				string n = a.Name.ToString();
				return n == EnumGenAttributeName || n == EnumGenAttributeAttributeName;
			})));

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
					.Where(x => x.AttributeClass != null && (x.AttributeClass.Name == EnumGenAttributeName || x.AttributeClass.Name == EnumGenAttributeAttributeName))
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
				if (symEnumMember == null)
				{
					context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(id: "ESG9999", title: "Failed to get symbol",
						messageFormat: "Could not get symbol for enum member {0}.",
						"category", DiagnosticSeverity.Error, isEnabledByDefault: true), Location.Create(enumMember.SyntaxTree, enumMember.Span), enumMember.Identifier.ToString()));
					continue;
				}
				var attribs = symEnumMember.GetAttributes();
				AttributeData? attrib = attribs
					.Where(x => x.AttributeClass != null && (x.AttributeClass.Name == NameAttributeName || x.AttributeClass.Name == NameAttributeAttributeName))
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
				enumData.Add(new EnumData(customId ?? id, id, customId == null));
			}

			StringBuilder sbExt_ToStr = new();
			sbExt_ToStr.Append("\t\tpublic static string ToStr(this ");
			sbExt_ToStr.Append(enumName);
			sbExt_ToStr.Append(" v)\n");
			sbExt_ToStr.Append("\t\t{\n");
			sbExt_ToStr.Append("\t\t\tswitch (v)\n");
			sbExt_ToStr.Append("\t\t\t{\n");

			StringBuilder sbEnum = new("#nullable enable\nnamespace ");
			sbEnum.Append(enumNamespace);
			sbEnum.Append("\n{\n");

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
				sbExt_ToStr.Append("\t\t\t\tcase ").Append(enumName).Append('.').Append(enumDatum.Identifier);
				sbExt_ToStr.Append(": return ").Append(fullEnumName).Append(";\n");

				sbEnum_NamesToValues.Append("\t\t\t[").Append(fullEnumName).Append("] = ");
				sbEnum_NamesToValues.Append(enumName).Append('.').Append(enumDatum.Identifier).Append(",\n");

				sbEnum_Values.Append(enumName).Append('.').Append(enumDatum.Identifier).Append(", ");
				sbEnum_Names.Append(fullEnumName).Append(", ");
			}

			sbEnum_Values.Append("};\n");
			sbEnum_Names.Append("};\n");
			sbEnum_NamesToValues.Append("\t\t};\n");
			sbExt_ToStr.Append("\t\t\t\tdefault: return string.Empty;\n");
			sbExt_ToStr.Append("\t\t\t}\n");
			sbExt_ToStr.Append("\t\t}\n");

			sbEnum.Append(sbEnum_Values);
			sbEnum.Append(sbEnum_Names);
			sbEnum.Append(sbEnum_NamesToValues);
			sbEnum.Append(sbExt_ToStr);

			sbEnum.Append("\t\tpublic static bool TryParse(string value, out ");
			sbEnum.Append(enumName);
			sbEnum.Append(" result) { return namesToValues.TryGetValue(value, out result); }\n");
			sbEnum.Append("\t\tpublic static System.Collections.Generic.IEnumerable<(string Name, ").Append(enumName).Append(" Value)> GetNameValues()\n");
			sbEnum.Append("\t\t{\n");
			sbEnum.Append("\t\t\tSystem.Diagnostics.Debug.Assert(names.Length == values.Length);\n");
			sbEnum.Append("\t\t\tfor(int i = 0; i < values.Length; ++i) { yield return (names[i], values[i]); }\n");
			sbEnum.Append("\t\t}\n");

			sbEnum.Append("\t\tpublic static System.ReadOnlySpan<string> NamesAsSpan => new System.ReadOnlySpan<string>(names);\n");
			sbEnum.Append("\t\tpublic static System.ReadOnlyMemory<string> NamesAsMemory => new System.ReadOnlyMemory<string>(names);\n");

			sbEnum.Append("\t\tpublic static System.ReadOnlySpan<").Append(enumName).Append("> ValuesAsSpan => new System.ReadOnlySpan<").Append(enumName).Append(">(values);\n");
			sbEnum.Append("\t\tpublic static System.ReadOnlyMemory<").Append(enumName).Append("> ValuesAsMemory => new System.ReadOnlyMemory<").Append(enumName).Append(">(values);\n");

			sbEnum.Append("\t}\n");
			sbEnum.Append("}\n");
			sbEnum.Append("#nullable restore");

			string sourceEnum = sbEnum.ToString();
			context.AddSource(enumClassName + ".g.cs", SourceText.From(sourceEnum, Encoding.UTF8));
		}
	}
}