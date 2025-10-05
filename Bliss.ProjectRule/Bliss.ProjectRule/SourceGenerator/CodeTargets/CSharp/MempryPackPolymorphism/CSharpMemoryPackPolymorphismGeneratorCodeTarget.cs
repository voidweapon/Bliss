
using Scriban;
using Scriban.Runtime;

namespace Bliss.ProjectRule.SourceGenerator.CodeTargets.CSharp;

internal class CSharpMemoryPackPolymorphismGeneratorCodeTarget : CSharpTemplateCodeTarget
{
    protected override void OnGenerateCode(TemplateContext tplCtx, GenerationContext context)
    {
        var polymorphismGenerationContext = context as SerializePolymorphismGenerationContext;
        var extraEnvs = new ScriptObject()
        {
            {KeywordDefine.ClassNameList, polymorphismGenerationContext.ClassNames},
        };
        tplCtx.PushGlobal(extraEnvs);
    }
}