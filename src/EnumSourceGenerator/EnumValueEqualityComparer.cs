namespace EnumSourceGenerator;

using System.Collections.Generic;
using System.Runtime.CompilerServices;

public sealed class EnumValueEqualityComparer : IEqualityComparer<EnumValue>
{
	public static readonly EnumValueEqualityComparer Default = new();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(EnumValue x, EnumValue y)
	{
		return x.Equals(y);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetHashCode(EnumValue obj)
	{
		return obj.GetHashCode();
	}
}
