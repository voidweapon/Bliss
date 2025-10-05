using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Bliss.ProjectRule.SourceGenerator.Generators;
using Bliss.ProjectRule.SourceGenerator.TemplateExtensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Scriban;
using Scriban.Runtime;

namespace Bliss.ProjectRule.SourceGenerator.CodeTargets;

internal abstract class TemplateCodeTargetBase
{
    protected TemplateContext CreateTemplateContext(Template template)
    {
        var ctx = new TemplateContext()
        {
            LoopLimit = 0,
            NewLine = "\n",
        };
        
        ctx.PushGlobal(new TypeTemplateExtension());
        OnCreateTemplateContext(ctx);
        return ctx;
    }

    protected virtual Template GetTemplate(string templateName, ref GeneratorExecutionContext executionContext)
    {
        if (TemplateManager.Instance.TryGetTemplate(templateName, out var template))
        {
            return template;
        }

        foreach (var additionalFile in executionContext.AdditionalFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(additionalFile.Path);
            if (fileName == templateName)
            {
               SourceText? sourceText =  additionalFile.GetText(executionContext.CancellationToken);
               if (sourceText != null)
               {
                   template = Template.Parse(sourceText.ToString());
                   TemplateManager.Instance.AddTemplate(templateName, template);
                   return template;
               }
            }
        }
        
        throw new Exception($"template:{templateName} not found in {TemplateManager.Instance.DebugSearch()}");
    }
    
    // public string GenerateCode(GenerationContext context, string templateName, ref GeneratorExecutionContext executionContext)
    // {
    //     var template = GetTemplate(templateName, ref executionContext);
    //     var tplCtx = CreateTemplateContext(template);
    //     var extraEnvs = new ScriptObject()
    //     {
    //         {KeywordDefine.Namespace, context.NameSpace},
    //         {KeywordDefine.ClassName, context.ClassName},
    //         {KeywordDefine.MethodName, context.MethodName},
    //         {KeywordDefine.GenericTypes, context.GenericTypes},
    //         {KeywordDefine.ReturnType, context.ReturnType},
    //     };
    //     tplCtx.PushGlobal(extraEnvs);
    //
    //     return template.Render(tplCtx);
    // }
    
    public string GenerateCode(GenerationContext context, string templateName, ref GeneratorExecutionContext executionContext)
    {
        var template = GetTemplate(templateName, ref executionContext);
        var tplCtx = CreateTemplateContext(template);
        var extraEnvs = new ScriptObject()
        {
            {KeywordDefine.Namespace, context.NameSpace},
            {KeywordDefine.ClassName, context.ClassName},
            {KeywordDefine.IsDotNet, context.IsDotNet},
        };
        tplCtx.PushGlobal(extraEnvs);
        OnGenerateCode(tplCtx, context);

        return template.Render(tplCtx);
    }

    protected virtual void OnGenerateCode(TemplateContext tplCtx, GenerationContext context)
    {
        
    }
    
    protected abstract void OnCreateTemplateContext(TemplateContext ctx);
}