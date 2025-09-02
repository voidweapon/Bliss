using System.Collections.Generic;
using System.Text;
using Scriban.Runtime;

namespace Bliss.ProjectRule.SourceGenerator.CodeTargets.CSharp;

internal class CsharpTemplateExtension : ScriptObject
{
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

    public static string generic_define(List<string> generic_types)
    {
        if (generic_types == null || generic_types.Count == 0)
        {
            return string.Empty;
        }
        return $"<{string.Join(",", generic_types)}>";
    }

    public static string generic_define_name(List<string> generic_types, int offset)
    {
        if (generic_types == null || generic_types.Count == 0)
        {
            return string.Empty;
        }
        StringBuilder sb = new StringBuilder();
        for (int i = offset; i < generic_types.Count; i++)
        {
            var generic_type =  generic_types[i];
            generic_type = generic_type.Replace('.', '_');
            sb.Append($"_{generic_type}");
        }
        
        return sb.ToString();
    }
    
    /// <summary>
    /// null -> ()
    /// T, K -> (T param0, K param1)
    /// </summary>
    /// <param name="generic_types"></param>
    /// <returns></returns>
    public static string generic_params_define(List<string> generic_types)
    {
        if (generic_types == null || generic_types.Count == 0)
        {
            return string.Empty;
        }
        StringBuilder sb = new StringBuilder();
        
        for (int i = 0; i < generic_types.Count; i++)
        {
            var generic_type =  generic_types[i];
            sb.Append($"{generic_type} param{i}");
            if (i != generic_types.Count - 1)
            {
                sb.Append(", ");
            }
        }
        return sb.ToString();
    }

    public static string generic_param_name(List<string> generic_types, int index)
    {
        return $"param{index}";
    }

    public static string generic_params_name(List<string> generic_types, int offset)
    {
        if (generic_types == null || generic_types.Count == 0 || offset >= generic_types.Count)
        {
            return string.Empty;
        }
        StringBuilder sb = new StringBuilder();
        
        for (int i = offset; i < generic_types.Count; i++)
        {
            sb.Append($"param{i}");
            if (i != generic_types.Count - 1)
            {
                sb.Append(", ");
            }
        }
        return sb.ToString();
    }
    

    public static string return_type(string in_return_type)
    {
        if (string.IsNullOrEmpty(in_return_type))
        {
            return "void";
        }
        
        return in_return_type;
    }
    
    public static string return_operate(string in_return_type)
    {
        if (string.IsNullOrEmpty(in_return_type))
        {
            return string.Empty;
        }
        
        return "return";
    }
}