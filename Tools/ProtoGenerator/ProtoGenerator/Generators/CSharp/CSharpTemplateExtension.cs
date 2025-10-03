using System.Text;
using Scriban.Runtime;

namespace ProtoGenerator.Generators;

/// <summary>
/// 提供Scriban模版引擎可调用的扩展函数
/// </summary>
public class CSharpTemplateExtension : ScriptObject
{
    private static CSharpSupportTypeDefine _supportTypeDefine =  new CSharpSupportTypeDefine();
    
    public static string namespace_with_grace_begin(string ns)
    {
        if (string.IsNullOrEmpty(ns))
        {
            return string.Empty;
        }
        return $"namespace {ns}\n{{";
    }

    public static string namespace_with_grace_end(string ns)
    {
        if (string.IsNullOrEmpty(ns))
        {
            return string.Empty;
        }
        return "}";
    }

    public static string gen_comment(string comment)
    {
        if (string.IsNullOrEmpty(comment))
        {
            return string.Empty;
        }
        return comment;
    }
    
    public static string base_class(List<string> parents)
    {
        if (parents == null || parents.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < parents.Count; i++)
        {
            sb.Append($", {parents[i]}");
        }
        return sb.ToString();
    }
    
    
    public static string clear_property(string propertyType, string propertyName)
    {
        EPropertyTypeCategory typeCategory =  _supportTypeDefine.GetTypeCategory(propertyType);
        switch (typeCategory)
        {
            case EPropertyTypeCategory.Literal:
                return $"this.{propertyName} = default;";
            case EPropertyTypeCategory.Collection:
                return $"this.{propertyName}.Clear();";
            default:
                throw new ArgumentOutOfRangeException();
        }
    }


    public static string gen_size_hint(ProtoClassInfo protoClassInfo)
    {
        StringBuilder sb = new StringBuilder();
        if (protoClassInfo.properties.Count == 0)
        {
            sb.Append("0");
        }
        else
        {
            for (int i = 0; i < protoClassInfo.properties.Count; i++)
            {
                var propertyInfo = protoClassInfo.properties[i];
                sb.Append($"sizeof({propertyInfo.property_type})");
                if (i != protoClassInfo.properties.Count - 1)
                {
                    sb.Append(" + ");
                }
            }
        }
        return sb.ToString();
    }

    
}