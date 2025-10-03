using ProtoGenerator.Generators;

namespace ProtoGenerator;

public class ProtoSourceGenerator
{
    public void Generate(Options options)
    {
        var generator = GeneratorFactor(options);
        
        if (!Directory.Exists(options.OutputPath))
        {
            Directory.CreateDirectory(options.OutputPath);
        }
        
        List<string> list = FileHelper.GetAllFiles(options.ProtoFilePath, "*proto");
        foreach (string filePath in list)
        {
            if (!filePath.EndsWith(".proto"))
            {
                continue;
            }
            
            string protoName = Path.GetFileNameWithoutExtension(filePath);
            GenProtoFile(filePath, protoName, generator, options);
        }
    }


    private void GenProtoFile(string protoFilePath, string protoName, BaseProtoGenerator generator, Options options)
    {
        ProtoGenContext genContext = GeneratorInfo(protoFilePath, options);
        
        string sourceText =  generator.Generate(genContext, options, protoName);
        
        string outputPath = Path.Combine(options.OutputPath, protoName);
        outputPath = $"{outputPath}.{generator.OutputFileExtension}";
        using FileStream txt = new(outputPath, FileMode.Create, FileAccess.ReadWrite);
        using StreamWriter sw = new(txt);
        sw.Write(sourceText);
        
        Console.WriteLine($"===================================================");
        Console.WriteLine($"Generate Message {protoName} Success");
        Console.WriteLine($"Output Path: {outputPath}");
        Console.WriteLine($"===================================================");
        
    }
    
    
    private BaseProtoGenerator GeneratorFactor(Options options)
    {
        switch (options.CodeTarget)
        {
            case ECodeTarget.cs_memorygepack:
                return new CSProtoGenerator();
            case ECodeTarget.cpp:
                throw new NotImplementedException();
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private ProtoGenContext GeneratorInfo(string protoFilePath, Options options)
    {
        ProtoGenContext result =  new ProtoSyntaxParser().Parser(protoFilePath);

        if (options.OpcodeGenMode == EOpcodeGenMode.Sequence)
        {
            int opcode = options.OpcodeMin;
            if (options.OpcodeOffsetMode == EOpcodeOffsetMode.Random)
            {
                opcode = new Random(Guid.NewGuid().GetHashCode() ^ Environment.TickCount).Next(options.OpcodeMin, options.OpcodeMax);
            }
            foreach (var message in result.proto_items)
            {
                message.opcode = opcode++;   
            }
        }
        else if(options.OpcodeGenMode == EOpcodeGenMode.HashCode)
        {
            foreach (var message in result.proto_items)
            {
                message.opcode = message.class_name.GetHashCode();
            }
        }
        
        return result;
    }
}