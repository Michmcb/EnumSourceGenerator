namespace EnumSourceGenerator;

public sealed class EnumData
{
	public EnumData(string name, string identifier, bool usesIdentifierAsName)
	{
		Name = name;
		Identifier = identifier;
		UsesIdentifierAsName = usesIdentifierAsName;
		/*, Dictionary<string, string?> stringRepresentations*/
		//StringRepresentations = stringRepresentations;
	}
	public string Name { get; }
	public string Identifier { get; }
	public bool UsesIdentifierAsName { get; }
	//public Dictionary<string, string?> StringRepresentations { get; }
}