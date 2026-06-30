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
    
    public static string define_property(ProtoPropertyInfo propertyInfo)
    {
        switch (propertyInfo.CollectionMode)
        {
            case ESupportedCollection.None:
            case ESupportedCollection.Array:
                return $"public {propertyInfo.property_type} {propertyInfo.name} {{ get; set; }}";
            case ESupportedCollection.List:
            case ESupportedCollection.Dictionary:
                return $"public {propertyInfo.property_type} {propertyInfo.name} {{ get; set; }} = new {propertyInfo.property_type}();";
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    public static string clear_property(string propertyType, string propertyName)
    {
        EPropertyTypeCategory typeCategory =  _supportTypeDefine.GetTypeCategory(propertyType);
        switch (typeCategory)
        {
            case EPropertyTypeCategory.Literal:
                return $"this.{propertyName} = default;";
            case EPropertyTypeCategory.Collection:
                return $"this.{propertyName}?.Clear();";
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

                if (propertyInfo.CollectionMode != ESupportedCollection.None
                    || propertyInfo.property_type == "string")
                {
                    continue;
                }
                
                if (!_propertySizeof.TryGetValue(propertyInfo.property_type, out var sizeofStr))
                {
                    sizeofStr = $"System.Runtime.CompilerServices.Unsafe.SizeOf<{propertyInfo.property_type}>()";
                }

                if (i != 0)
                {
                    sb.Append(" + ");
                }
                sb.Append(sizeofStr);
            }
        }
        return sb.ToString();
    }

    private static Dictionary<string, string> _propertySizeof = new Dictionary<string, string>()
    {
        {"Unity.Mathematics.float2", "2 * sizeof(float)"},
        {"Unity.Mathematics.float3", "3 * sizeof(float)"},
        {"Unity.Mathematics.float4", "4 * sizeof(float)"},
        {"Unity.Mathematics.quaternion", "4 * sizeof(float)"},
        {"Unity.Mathematics.half",  "sizeof(ushort)"},
        {"Unity.Mathematics.half2", "2 * sizeof(ushort)"},
        {"Unity.Mathematics.half3", "3 * sizeof(ushort)"},
        {"Unity.Mathematics.half4", "4 * sizeof(ushort)"},
        {"Unity.Mathematics.double2", "2 * sizeof(double)"},
        {"Unity.Mathematics.double3", "3 * sizeof(double)"},
        {"Unity.Mathematics.double4", "4 * sizeof(double)"},
        {"Unity.Mathematics.int2", "2 * sizeof(int)"},
        {"Unity.Mathematics.int3", "3 * sizeof(int)"},
        {"Unity.Mathematics.int4", "4 * sizeof(int)"},
        {"Unity.Mathematics.bool2", "2 * sizeof(bool)"},
        {"Unity.Mathematics.bool3", "3 * sizeof(bool)"},
        {"Unity.Mathematics.bool4", "4 * sizeof(bool)"},
    };


}