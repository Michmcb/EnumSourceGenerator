namespace EnumSourceGenerator;

using System.Collections.Generic;
using System.Runtime.CompilerServices;

public sealed class EnumNameValueEqualityComparer : IEqualityComparer<EnumNameValue>
{
	public static readonly EnumNameValueEqualityComparer Default = new();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(EnumNameValue x, EnumNameValue y)
	{
		return x.Equals(y);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetHashCode(EnumNameValue obj)
	{
		return obj.GetHashCode();
	}
}
