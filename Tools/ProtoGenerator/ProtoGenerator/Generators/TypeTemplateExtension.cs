using Scriban.Runtime;

namespace ProtoGenerator.Generators;

internal class TypeTemplateExtension : ScriptObject
{
    public static string escape_comment(string comment)
    {
        return System.Web.HttpUtility.HtmlEncode(comment).Replace("\n", "<br/>");
    }
}