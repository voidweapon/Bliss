using System.Collections.Generic;

namespace Bliss.ProjectRule.SourceGenerator;

public class GenerationContext
{
    public string NameSpace { get; set; }
    public string ClassName { get; set; }
    
    public string MethodName { get; set; }
    
    public List<string> GenericTypes { get; set; }
    
    public string ReturnType { get; set; }
    
    public bool IsDotNet { get; set; }
}