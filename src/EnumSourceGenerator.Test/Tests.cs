namespace EnumSourceGenerator.Test
{
	using System;

	public static class Tests
	{
		[Fact]
		public static void ToStr()
		{
			string[] names = Enum.GetNames<TestEnum>();
			TestEnum[] values = Enum.GetValues<TestEnum>();
			for (int i = 0; i < names.Length; i++)
			{
				Assert.Equal(names[i], values[i].ToStr());
			}
			Assert.Equal(((TestEnum)0).ToString(), ((TestEnum)0).ToStr());

			TestEnum x = TestEnum.One | TestEnum.Two;
			Assert.Equal(x.ToString().Replace(", ", "|"), x.ToStr());
		}
		[Fact]
		public static void TryParse()
		{
			string[] names = Enum.GetNames<TestEnum>();
			TestEnum[] values = Enum.GetValues<TestEnum>();
			for (int i = 0; i < names.Length; i++)
			{
				Assert.True(EnumTestEnum.TryParse(names[i], out TestEnum v));
				Assert.Equal(values[i], v);
			}

			Assert.False(EnumTestEnum.TryParse("kghdjfkdfg", out var v1));
			Assert.Equal(default, v1);

			Assert.False(EnumTestEnum.TryParse("", out var v2));
			Assert.Equal(default, v2);

			Assert.False(EnumTestEnum.TryParse(null, out var v3));
			Assert.Equal(default, v3);
		}
		[Fact]
		public static void HasFlag()
		{
			TestEnum f = TestEnum.One | TestEnum.Two;
			Assert.Equal(f.HasFlag(TestEnum.One), (f & TestEnum.One) == TestEnum.One);
		}
		[Fact]
		public static void Parse()
		{
			string[] names = Enum.GetNames<TestEnum>();
			TestEnum[] values = Enum.GetValues<TestEnum>();
			for (int i = 0; i < names.Length; i++)
			{
				Assert.Equal(values[i], EnumTestEnum.Parse(names[i]));
			}

			Assert.Throws<ArgumentException>(() => EnumTestEnum.Parse("kghdjfkdfg"));

			Assert.Throws<ArgumentException>(() => EnumTestEnum.Parse(""));

			Assert.Throws<ArgumentException>(() => EnumTestEnum.Parse(null));
		}
		[Fact]
		public static void GetNames()
		{
			string[] names1 = Enum.GetNames<TestEnum>();
			ReadOnlyMemory<string> names2 = EnumTestEnum.NamesAsMemory;
			Assert.Equal(names1.Length, names2.Length);
			for (int i = 0; i < names1.Length; i++)
			{
				Assert.Equal(names1[i], names2.Span[i]);
			}
			ReadOnlySpan<string> names3 = EnumTestEnum.NamesAsSpan;
			Assert.Equal(names1.Length, names3.Length);
			for (int i = 0; i < names1.Length; i++)
			{
				Assert.Equal(names1[i], names3[i]);
			}
		}
		[Fact]
		public static void GetValues()
		{
			TestEnum[] values1 = Enum.GetValues<TestEnum>();
			ReadOnlyMemory<TestEnum> values2 = EnumTestEnum.ValuesAsMemory;
			Assert.Equal(values1.Length, values2.Length);
			for (int i = 0; i < values1.Length; i++)
			{
				Assert.Equal(values1[i], values2.Span[i]);
			}
			ReadOnlySpan<TestEnum> values3 = EnumTestEnum.ValuesAsSpan;
			Assert.Equal(values1.Length, values3.Length);
			for (int i = 0; i < values1.Length; i++)
			{
				Assert.Equal(values1[i], values3[i]);
			}
		}
		[Fact]
		public static void GetRawValues()
		{
			long[] values1 = (long[])Enum.GetValuesAsUnderlyingType<TestEnum>();
			ReadOnlyMemory<long> values2 = EnumTestEnum.UnderlyingValuesAsMemory;
			Assert.Equal(values1.Length, values2.Length);
			for (int i = 0; i < values1.Length; i++)
			{
				Assert.Equal(values1[i], values2.Span[i]);
			}
			ReadOnlySpan<long> values3 = EnumTestEnum.UnderlyingValuesAsSpan;
			Assert.Equal(values1.Length, values3.Length);
			for (int i = 0; i < values1.Length; i++)
			{
				Assert.Equal(values1[i], values3[i]);
			}
		}
		[Fact]
		public static void GetNameValues()
		{
			string[] names = Enum.GetNames<TestEnum>();
			TestEnum[] values = Enum.GetValues<TestEnum>();
			int i = 0;
			foreach ((string Name, TestEnum Value) in EnumTestEnum.GetNameValues())
			{
				Assert.Equal(names[i], Name);
				Assert.Equal(values[i], Value);
				++i;
			}
			i = 0;
			foreach ((string Name, long Value) in EnumTestEnum.GetNameUnderlyingValues())
			{
				Assert.Equal(names[i], Name);
				Assert.Equal((long)values[i], Value);
				++i;
			}
		}
		[Fact]
		public static void IsDefined()
		{
			TestEnum[] values = Enum.GetValues<TestEnum>();
			for (int i = 0; i < values.Length; i++)
			{
				Assert.True(values[i].IsDefined());
			}
			Assert.False(((TestEnum)49238).IsDefined());
		}
		[Fact]
		public static void UnderlyingType()
		{
			Assert.Equal(Enum.GetUnderlyingType(typeof(TestEnum)), EnumTestEnum.UnderlyingType);
		}
	}
}