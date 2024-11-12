namespace EnumSourceGenerator;
using System;
public readonly struct EnumNode : IEquatable<EnumNode>
{
	public EnumNode(string? containingNamespace, string name, bool isFlags, EnumValueType type, StringComparison comparison, EqArr<EnumNameValue> members)
	{
		ContainingNamespace = containingNamespace;
		Name = name;
		IsFlags = isFlags;
		Type = type;
		Comparison = comparison;
		Members = members;
	}
	public readonly string? ContainingNamespace;
	public readonly string Name;
	public readonly bool IsFlags;
	public readonly EnumValueType Type;
	public readonly StringComparison Comparison;
	public readonly EqArr<EnumNameValue> Members;
	public override bool Equals(object? obj)
	{
		return obj is EnumNode node && Equals(node);
	}
	public bool Equals(EnumNode other)
	{
		return ContainingNamespace == other.ContainingNamespace
			&& Name == other.Name
			&& IsFlags == other.IsFlags
			&& Type == other.Type
			&& Comparison == other.Comparison
			&& Members.Equals(other.Members);
	}
	public override int GetHashCode()
	{
		int hashCode = -1545771430;
		hashCode = hashCode * -1521134295 + StringComparer.Ordinal.GetHashCode(ContainingNamespace);
		hashCode = hashCode * -1521134295 + StringComparer.Ordinal.GetHashCode(Name);
		hashCode = hashCode * -1521134295 + IsFlags.GetHashCode();
		hashCode = hashCode * -1521134295 + Type.GetHashCode();
		hashCode = hashCode * -1521134295 + Comparison.GetHashCode();
		hashCode = hashCode * -1521134295 + Members.GetHashCode();
		return hashCode;
	}
	public static bool operator ==(EnumNode left, EnumNode right) => left.Equals(right);
	public static bool operator !=(EnumNode left, EnumNode right) => !(left == right);
}
