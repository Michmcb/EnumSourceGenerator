namespace EnumSourceGenerator;

using System;
public sealed class EnumNameValue : IEquatable<EnumNameValue?>
{
	public EnumNameValue(string identifier, string name, bool hasCustomName, EnumValue value)
	{
		Identifier = identifier;
		Name = name;
		HasCustomName = hasCustomName;
		Value = value;
	}
	public readonly string Identifier;
	public readonly string Name;
	public readonly bool HasCustomName;
	public readonly EnumValue Value;
	public override bool Equals(object? obj)
	{
		return Equals(obj as EnumNameValue);
	}
	public bool Equals(EnumNameValue? other)
	{
		return other is not null &&
			Identifier == other.Identifier &&
			Name == other.Name &&
			HasCustomName == other.HasCustomName &&
			Value.Equals(other.Value);
	}
	public static bool Equals(EnumNameValue? lhs, EnumNameValue? rhs)
	{
		if (lhs is null) { return rhs is null; }
		if (ReferenceEquals(lhs, rhs)) return true;
		return lhs.Equals(rhs);
	}
	public override int GetHashCode()
	{
		int hashCode = 1307928041;
		hashCode = hashCode * -1521134295 + StringComparer.Ordinal.GetHashCode(Identifier);
		hashCode = hashCode * -1521134295 + StringComparer.Ordinal.GetHashCode(Name);
		hashCode = hashCode * -1521134295 + HasCustomName.GetHashCode();
		hashCode = hashCode * -1521134295 + Value.GetHashCode();
		return hashCode;
	}
	public static bool operator ==(EnumNameValue? left, EnumNameValue? right) => Equals(left, right);
	public static bool operator !=(EnumNameValue? left, EnumNameValue? right) => !(left == right);
}
