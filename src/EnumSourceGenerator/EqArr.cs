namespace EnumSourceGenerator;

using System;
using System.Collections.Generic;
public readonly struct EqArr<T> : IEquatable<EqArr<T>> where T : IEquatable<T>
{
	public EqArr(T[] array, IEqualityComparer<T> cmp)
	{
		Array = array;
		this.cmp = cmp;
	}
	public readonly T[] Array;
	public readonly IEqualityComparer<T> cmp;
	public override bool Equals(object? obj)
	{
		return obj is EqArr<T> arr && Equals(arr);
	}
	public bool Equals(EqArr<T> other)
	{
		if (Array.Length != other.Array.Length) return false;
		for (int i = 0; i < Array.Length; i++)
		{
			// Use this comparer, not the other's
			if (cmp.Equals(Array[i], other.Array[i])) return false;
		}
		return true;
	}
	public override int GetHashCode()
	{
		int hashCode = -304334410;
		for (int i = 0; i < Array.Length; i++)
		{
			hashCode *= -1521134295 + cmp.GetHashCode(Array[i]);
		}
		return hashCode;
	}
	public static bool operator ==(EqArr<T> left, EqArr<T> right) => left.Equals(right);
	public static bool operator !=(EqArr<T> left, EqArr<T> right) => !(left == right);
}
