using System.Text.RegularExpressions;

namespace ProtoGenerator.Generators;

public class CSharpSupportTypeDefine : SupportTypeDefine
{
    private readonly static HashSet<string> LiteralTypes = new HashSet<string>()
    {
        "bool",
        "byte",
        "short",
        "ushort",
        "int",
        "uint",
        "long",
        "ulong",
        "float",
        "double",
        "decimal",
        "char",
        "string",
    };
    
    private Regex _collectionRegex =  new Regex("List<[^>]+>|Dictionary<[^,]+,[^>]+>");
    
    public override string ConvertType(string typeName)
    {
        return typeName switch
        {
            "int16" => "short",
            "int32" => "int",
            "bytes" => "byte[]",
            "uint32" => "uint",
            "long" => "long",
            "int64" => "long",
            "uint64" => "ulong",
            "uint16" => "ushort",
            _ => typeName
        };
    }

    public override string ConvertCollectionType(ESupportedCollection collection, string type)
    {
        return collection switch
        {
            ESupportedCollection.List => $"List<{type}>",
            _ => type
        };
    }

    public override EPropertyTypeCategory GetTypeCategory(string propertyType)
    {
        bool isCollection = _collectionRegex.IsMatch(propertyType);
        if (isCollection)
        {
            return EPropertyTypeCategory.Collection;
        }

        return EPropertyTypeCategory.Literal;
    }
}