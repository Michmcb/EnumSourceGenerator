namespace EnumSourceGenerator;

public sealed class EnumData
{
	public EnumData(string name, string identifier, bool usesIdentifierAsName, string valueAsString)
	{
		Name = name;
		Identifier = identifier;
		UsesIdentifierAsName = usesIdentifierAsName;
		ValueAsString = valueAsString;
		/*, Dictionary<string, string?> stringRepresentations*/
		//StringRepresentations = stringRepresentations;
	}
	public string Name { get; }
	public string Identifier { get; }
	public bool UsesIdentifierAsName { get; }
	public string ValueAsString { get; }
	//public Dictionary<string, string?> StringRepresentations { get; }
}