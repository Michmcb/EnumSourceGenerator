namespace EnumSourceGenerator.Test
{
	using System;

	[EnumGen]
	[Flags]
	public enum TestEnum : long
	{
		None,
		One = 1,
		Two = 2,
		Three = 4,
		Four = 8,
		Five = 16,
	}
}