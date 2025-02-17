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
[Generator(LanguageNames.CSharp)]
public sealed class SourceGen : IIncrementalGenerator
{
	public const string EnumGenAttributeName = "EnumGen";
	public const string NameAttributeName = "Name";
	// TODO is there any way rather than this crap to detect our attribute reliably? I mean, they can always use a using statement to alias it and then we're screwed...
	private static readonly HashSet<string> EnumGenAttributeNames = [EnumGenAttributeName, EnumGenAttributeName + "Attribute", "EnumSourceGenerator." + EnumGenAttributeName, "EnumSourceGenerator." + EnumGenAttributeName + "Attribute"];
	private static readonly HashSet<string> NameAttributeNames = [NameAttributeName, NameAttributeName + "Attribute", "EnumSourceGenerator." + NameAttributeName, "EnumSourceGenerator." + NameAttributeName + "Attribute"];

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		// Calling this method for our attributes allows us to access the constructor arguments by using symbols in the execute method below
		context.RegisterPostInitializationOutput(static (i) => i.AddSource("EnumSourceGeneratorAttributes.g.cs", SourceText.From("namespace EnumSourceGenerator\n" +
"{\n" +
"\tusing System;\n" +

"\t/// <summary>\n" +
"\t/// Decorating an enum with this attribute will cause various methods to be generated for it.\n" +
"\t/// </summary>\n" +
"\t[AttributeUsage(AttributeTargets.Enum)]\n" +
"\tinternal sealed class " + EnumGenAttributeName + "Attribute : Attribute\n" +
"\t{\n" +
"\t\tinternal EnumGenAttribute(StringComparison comparer = StringComparison.Ordinal)\n" +
"\t\t{\n" +
"\t\t\tComparer = comparer;\n" +
"\t\t}\n" +
"\t\tinternal StringComparison Comparer { get; }\n" +
"\t}\n" +

"\t/// <summary>\n" +
"\t/// Decorating an enum member with this attribute will define its string representation.\n" +
"\t/// </summary>\n" +
"\t[AttributeUsage(AttributeTargets.Field)]\n" +
"\tinternal sealed class " + NameAttributeName + "Attribute : Attribute\n" +
"\t{\n" +
"\t\tinternal NameAttribute(string text)\n" +
"\t\t{\n" +
"\t\t\tText = text;\n" +
"\t\t}\n" +
"\t\tinternal string Text { get; }\n" +
"\t}\n" +
"}\n", Encoding.UTF8)));

		//#if DEBUG
		//if (!System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Launch();
		//#endif

		var values = context.SyntaxProvider.ForAttributeWithMetadataName("EnumSourceGenerator.EnumGenAttribute",
			predicate: (s, ct) => s is EnumDeclarationSyntax,
			transform: GetNodes);

		context.RegisterSourceOutput(values, Generate);
	}
	private EnumNode GetNodes(GeneratorAttributeSyntaxContext context, CancellationToken ct)
	{
		bool isFlags = false;
		if (context.TargetSymbol is INamedTypeSymbol nts)
		{
			SpecialType? mUnderlyingType = nts.EnumUnderlyingType?.SpecialType;
			if (mUnderlyingType.HasValue)
			{
				StringComparison comparison = StringComparison.Ordinal;
				foreach (AttributeData attrib in context.TargetSymbol.GetAttributes())
				{
					if (attrib.AttributeClass is not null)
					{
						if (EnumGenAttributeNames.Contains(attrib.AttributeClass.Name))
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
						else if (attrib.ToString() == "System.FlagsAttribute")
						{
							isFlags = true;
						}
					}
				}
				var underlyingType = mUnderlyingType.Value;
				var members = nts.GetMembers();
				EnumNameValue[] nvs = new EnumNameValue[members.Length];
				int i = 0;
				foreach (var m in members)
				{
					if (m is IFieldSymbol f && f.HasConstantValue)
					{
						string identifier = f.Name;
						string name = f.Name;
						bool hasCustomName = false;
						foreach (AttributeData attrib in m.GetAttributes())
						{
							if (attrib.AttributeClass != null && NameAttributeNames.Contains(attrib.AttributeClass.Name))
							{
								ImmutableArray<TypedConstant> attribArgs = attrib.ConstructorArguments;
								if (attribArgs.Length == 1)
								{
									object? o = attribArgs[0].Value;
									if (!(o is null) && o is string str)
									{
										hasCustomName = true;
										name = str.Replace("\"", "\\\"");
										break;
									}
								}
							}
						}
						switch (underlyingType)
						{
							case SpecialType.System_SByte:
								nvs[i++] = new(identifier, name, hasCustomName, new((sbyte)f.ConstantValue));
								break;
							case SpecialType.System_Byte:
								nvs[i++] = new(identifier, name, hasCustomName, new((byte)f.ConstantValue));
								break;
							case SpecialType.System_Int16:
								nvs[i++] = new(identifier, name, hasCustomName, new((short)f.ConstantValue));
								break;
							case SpecialType.System_UInt16:
								nvs[i++] = new(identifier, name, hasCustomName, new((ushort)f.ConstantValue));
								break;
							case SpecialType.System_Int32:
								nvs[i++] = new(identifier, name, hasCustomName, new((int)f.ConstantValue));
								break;
							case SpecialType.System_UInt32:
								nvs[i++] = new(identifier, name, hasCustomName, new((uint)f.ConstantValue));
								break;
							case SpecialType.System_Int64:
								nvs[i++] = new(identifier, name, hasCustomName, new((long)f.ConstantValue));
								break;
							case SpecialType.System_UInt64:
								nvs[i++] = new(identifier, name, hasCustomName, new((ulong)f.ConstantValue));
								break;
						}
					}
				}
				if (nvs.Length != i)
				{
					Array.Resize(ref nvs, i);
				}
				return new EnumNode(context.TargetSymbol.ContainingNamespace?.ToString(), context.TargetSymbol.Name, isFlags, (EnumValueType)underlyingType, comparison, new(nvs));
			}
		}
		return new EnumNode(context.TargetSymbol.ContainingNamespace?.ToString(), context.TargetSymbol.Name, isFlags, default, StringComparison.Ordinal, default);
	}
	private void Generate(SourceProductionContext context, EnumNode source)
	{
		string? enumNamespace = source.ContainingNamespace;
		string enumName = source.Name;
		string underlyingType;
		switch (source.Type)
		{
			case EnumValueType.SByte:
				underlyingType = "sbyte";
				break;
			case EnumValueType.Byte:
				underlyingType = "byte";
				break;
			case EnumValueType.Short:
				underlyingType = "short";
				break;
			case EnumValueType.UShort:
				underlyingType = "ushort";
				break;
			default:
			case EnumValueType.Int:
				underlyingType = "int";
				break;
			case EnumValueType.UInt:
				underlyingType = "uint";
				break;
			case EnumValueType.Long:
				underlyingType = "long";
				break;
			case EnumValueType.ULong:
				underlyingType = "ulong";
				break;
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
		switch (source.Comparison)
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

		foreach (var enumDatum in source.Members.Array)
		{
			string fullEnumName = enumDatum.HasCustomName
				? string.Concat("\"", enumDatum.Name, "\"")
				: string.Concat("nameof(", enumName, ".", enumDatum.Identifier, ")");
			sbEnum_ToStr.Append("\t\t\t\tcase ").Append(enumName).Append('.').Append(enumDatum.Identifier);
			sbEnum_ToStr.Append(": return ").Append(fullEnumName).Append(";\n");

			sbEnum_IsDefined.Append("\t\t\t\tcase ").Append(enumName).Append('.').Append(enumDatum.Identifier).Append(":\n");

			sbEnum_NamesToValues.Append("\t\t\t[").Append(fullEnumName).Append("] = ");
			sbEnum_NamesToValues.Append(enumName).Append('.').Append(enumDatum.Identifier).Append(",\n");

			sbEnum_Values.Append(enumName).Append('.').Append(enumDatum.Identifier).Append(", ");
			EnumValue value = enumDatum.Value;
			switch (source.Type)
			{
				case EnumValueType.SByte:
					sbEnum_UnderlyingValues.Append(value.SByte);
					break;
				case EnumValueType.Byte:
					sbEnum_UnderlyingValues.Append(value.Byte);
					break;
				case EnumValueType.Short:
					sbEnum_UnderlyingValues.Append(value.Short);
					break;
				case EnumValueType.UShort:
					sbEnum_UnderlyingValues.Append(value.UShort);
					break;
				default:
				case EnumValueType.Int:
					sbEnum_UnderlyingValues.Append(value.Int);
					break;
				case EnumValueType.UInt:
					sbEnum_UnderlyingValues.Append(value.UInt);
					break;
				case EnumValueType.Long:
					sbEnum_UnderlyingValues.Append(value.Long);
					break;
				case EnumValueType.ULong:
					sbEnum_UnderlyingValues.Append(value.ULong);
					break;
			}
			sbEnum_UnderlyingValues.Append(", ");
			sbEnum_Names.Append(fullEnumName).Append(", ");
		}

		sbEnum_Values.Append("};\n");
		sbEnum_UnderlyingValues.Append("};\n");
		sbEnum_Names.Append("};\n");
		sbEnum_NamesToValues.Append("\t\t};\n");

		if (source.IsFlags)
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

		if (source.IsFlags)
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
			sbEnum.Append("\t\tpublic static bool HasFlag(this ");
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