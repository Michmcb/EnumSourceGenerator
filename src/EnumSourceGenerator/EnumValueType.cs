namespace EnumSourceGenerator;

using Microsoft.CodeAnalysis;

public enum EnumValueType
{
	SByte = SpecialType.System_SByte,
	Byte = SpecialType.System_Byte,
	Short = SpecialType.System_Int16,
	UShort = SpecialType.System_UInt16,
	Int = SpecialType.System_Int32,
	UInt = SpecialType.System_UInt32,
	Long = SpecialType.System_Int64,
	ULong = SpecialType.System_UInt64,
}
