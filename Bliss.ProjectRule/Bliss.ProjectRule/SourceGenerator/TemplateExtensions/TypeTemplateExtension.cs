using Scriban.Runtime;

namespace Bliss.ProjectRule.SourceGenerator.TemplateExtensions;

internal class TypeTemplateExtension : ScriptObject
{
    public static string escape_comment(string comment)
    {
        return System.Web.HttpUtility.HtmlEncode(comment).Replace("\n", "<br/>");
    }
}