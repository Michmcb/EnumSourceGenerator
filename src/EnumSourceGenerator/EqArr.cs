namespace EnumSourceGenerator;

using System;
using System.Linq;

public readonly struct EqArr<T> : IEquatable<EqArr<T>> where T : IEquatable<T>
{
	public EqArr(T[] array)
	{
		Array = array;
	}
	public readonly T[] Array;
	public override bool Equals(object? obj)
	{
		return obj is EqArr<T> arr && Equals(arr);
	}
	public bool Equals(EqArr<T> other)
	{
		return Array.AsSpan().SequenceEqual(other.Array.AsSpan());
	}
	public override int GetHashCode()
	{
		int hashCode = -304334410;
		for (int i = 0; i < Array.Length; i++)
		{
			hashCode *= -1521134295 + Array[i].GetHashCode();
		}
		return hashCode;
	}
	public static bool operator ==(EqArr<T> left, EqArr<T> right) => left.Equals(right);
	public static bool operator !=(EqArr<T> left, EqArr<T> right) => !(left == right);
}
