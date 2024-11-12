namespace EnumSourceGenerator;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public sealed class EqArrEqualityComparer<T> : IEqualityComparer<EqArr<T>> where T : IEquatable<T>
{
	public static readonly EqArrEqualityComparer<T> Default = new();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(EqArr<T> x, EqArr<T> y)
	{
		return x.Equals(y);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetHashCode(EqArr<T> obj)
	{
		return obj.GetHashCode();
	}
}
