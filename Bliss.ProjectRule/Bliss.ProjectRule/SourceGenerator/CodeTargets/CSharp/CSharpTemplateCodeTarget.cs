using Scriban;

namespace Bliss.ProjectRule.SourceGenerator.CodeTargets.CSharp;

internal class CSharpTemplateCodeTarget : TemplateCodeTargetBase
{
    protected override void OnCreateTemplateContext(TemplateContext ctx)
    {
        ctx.PushGlobal(new CsharpTemplateExtension());
    }
}