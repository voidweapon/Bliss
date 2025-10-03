using Scriban;

namespace ProtoGenerator.Generators;

public class CSProtoGenerator : BaseProtoGenerator
{
    public override string OutputFileExtension => "cs";
    protected override void OnCreateTemplateContext(TemplateContext ctx)
    {
        ctx.PushGlobal(new CSharpTemplateExtension());
    }
}