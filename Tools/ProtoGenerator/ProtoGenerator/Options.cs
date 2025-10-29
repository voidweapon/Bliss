using CommandLine;

namespace ProtoGenerator;

public class Options
{
    [Option('p', "path", Required = true, HelpText = "Proto file path")]
    public string ProtoFilePath { get; set; }
    
    [Option('o', "output", Required = true, HelpText = "Generated file output path")]
    public string OutputPath { get; set; }
    
    [Option('c', "target", Required = true, HelpText = "Gen code type")]
    public ECodeTarget  CodeTarget { get; set; }
    
    [Option('g', "opcode", Required = true, HelpText = "Gen opcode Mode", Default = EOpcodeGenMode.Sequence)]
    public EOpcodeGenMode  OpcodeGenMode { get; set; }
    
    [Option("offset_min", Required = false, HelpText = "Gen opcode Sequence Mode code start offset min", Default = 0)]
    public ushort OpcodeMin { get; set; }
    
    [Option("offset_max", Required = false, HelpText = "Gen opcode Sequence Mode code start offset max", Default = 0)]
    public ushort OpcodeMax { get; set; }
    
    [Option('m', "offset_mode", Required = false, HelpText = "Gen opcode Sequence Mode code start offset", Default = EOpcodeOffsetMode.Specific)]
    public EOpcodeOffsetMode OpcodeOffsetMode { get; set; }
}

public enum EOpcodeGenMode : int
{
    Sequence = 0,
    HashCode = 1,
}

public enum EOpcodeOffsetMode : int
{
    Specific,
    Random,
}

public enum ECodeTarget
{
    /// <summary>
    /// CSharp
    /// </summary>
    cs_memorygepack,
    
    /// <summary>
    /// C++
    /// </summary>
    cpp,
}