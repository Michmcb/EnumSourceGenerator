namespace EnumSourceGenerator;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public sealed class EnumNodeEqualityComparer : IEqualityComparer<EnumNode>
{
	public static readonly EnumNodeEqualityComparer Default = new();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(EnumNode x, EnumNode y)
	{
		return x.Equals(y);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetHashCode(EnumNode obj)
	{
		return obj.GetHashCode();
	}
}
