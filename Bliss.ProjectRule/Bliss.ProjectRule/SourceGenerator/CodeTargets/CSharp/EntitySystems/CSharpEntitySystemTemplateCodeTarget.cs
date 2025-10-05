using Scriban;
using Scriban.Runtime;

namespace Bliss.ProjectRule.SourceGenerator.CodeTargets.CSharp;

internal class CSharpEntitySystemTemplateCodeTarget : CSharpTemplateCodeTarget
{
    protected override void OnGenerateCode(TemplateContext tplCtx, GenerationContext context)
    {
        var entitySystemContext = context as EntitySystemsGenerationContext;
        var extraEnvs = new ScriptObject()
        {
            {KeywordDefine.EntitySystems, entitySystemContext.defEntitySystems},
        };
        tplCtx.PushGlobal(extraEnvs);
    }
}