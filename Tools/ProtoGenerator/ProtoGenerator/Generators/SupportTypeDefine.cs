namespace ProtoGenerator.Generators;

public abstract class SupportTypeDefine
{
    public abstract string ConvertType(string typeName);
    
    public abstract string ConvertCollectionType(ESupportedCollection collection, string type);

    public abstract EPropertyTypeCategory GetTypeCategory(string propertyType);
}

public enum EPropertyTypeCategory
{
    Literal,
    Collection,
}