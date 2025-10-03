using Scriban;
using Scriban.Runtime;

namespace ProtoGenerator.Generators;

public abstract class BaseProtoGenerator
{
    public abstract string OutputFileExtension { get; }
    
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
    
    protected abstract void OnCreateTemplateContext(TemplateContext ctx);

    protected Template GetTemplate(ECodeTarget target)
    {
        string? name = Enum.GetName(typeof(ECodeTarget), target);
        if (name == null)
        {
            throw new ArgumentException($"Unknown target: {target}");
        }

        var curDir = Path.GetDirectoryName(AppContext.BaseDirectory);
        string templatePath = Path.Combine(Path.Combine($"{curDir}/Templates", name), "message_proto.sbn");
        var sourceText = File.ReadAllText(templatePath);
        return Template.Parse(sourceText);
    }
    
    public string Generate(ProtoGenContext genContext, Options options, string protoName)
    {
        var template = GetTemplate(options.CodeTarget);
        var tplCtx = CreateTemplateContext(template);
        var extraEnvs = new ScriptObject()
        {
            {TemplateKeywordDefine.IncludeFiles, genContext.includes},
            {TemplateKeywordDefine.Namespace, genContext.namesapce},
            {TemplateKeywordDefine.ProtoMessages, genContext.proto_items},
            {TemplateKeywordDefine.FileName, protoName},
        };
        tplCtx.PushGlobal(extraEnvs);
        
        return template.Render(tplCtx);
    }

}