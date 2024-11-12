namespace EnumSourceGenerator;

using System;
using System.Runtime.InteropServices;
[StructLayout(LayoutKind.Explicit)]
public readonly struct EnumValue : IEquatable<EnumValue>
{
	public EnumValue(sbyte value)
	{
		ULong = 0;
		SByte = value;
	}
	public EnumValue(byte value)
	{
		ULong = 0;
		Byte = value;
	}
	public EnumValue(short value)
	{
		ULong = 0;
		Short = value;
	}
	public EnumValue(ushort value)
	{
		ULong = 0;
		UShort = value;
	}
	public EnumValue(int value)
	{
		ULong = 0;
		Int = value;
	}
	public EnumValue(uint value)
	{
		ULong = 0;
		UInt = value;
	}
	public EnumValue(long value)
	{
		Long = value;
	}
	public EnumValue(ulong value)
	{
		ULong = value;
	}
	[FieldOffset(0)]
	public readonly sbyte SByte;
	[FieldOffset(0)]
	public readonly byte Byte;
	[FieldOffset(0)]
	public readonly short Short;
	[FieldOffset(0)]
	public readonly ushort UShort;
	[FieldOffset(0)]
	public readonly int Int;
	[FieldOffset(0)]
	public readonly uint UInt;
	[FieldOffset(0)]
	public readonly long Long;
	[FieldOffset(0)]
	public readonly ulong ULong;
	public override bool Equals(object? obj)
	{
		return obj is EnumValue value && Equals(value);
	}
	public bool Equals(EnumValue other)
	{
		return ULong == other.ULong;
	}
	public override int GetHashCode()
	{
		int hashCode = 1875718729;
		hashCode = hashCode * -1521134295 + ULong.GetHashCode();
		return hashCode;
	}
	public static bool operator ==(EnumValue left, EnumValue right) => left.Equals(right);
	public static bool operator !=(EnumValue left, EnumValue right) => !(left == right);
}
