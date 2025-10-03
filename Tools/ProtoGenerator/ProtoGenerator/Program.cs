using CommandLine;
namespace ProtoGenerator;

class Program
{
    static void Main(string[] args)
    {
        Options options = null;
        Parser.Default.ParseArguments<Options>(args)
            .WithParsed(o => options = o)
            .WithNotParsed(err => throw new Exception($"Command line error:{err}"));

        if (options != null)
        {
            options.ProtoFilePath = Path.GetFullPath(options.ProtoFilePath);
            options.OutputPath = Path.GetFullPath(options.OutputPath);
            new ProtoSourceGenerator().Generate(options);
        }
    }
}