using System.Collections.Generic;

namespace Bliss.ProjectRule.SourceGenerator.Generators;

public class DefEntitySystem
{
    public string method_name { get; set; }
    
    public List<string> generic_types { get; set; }
    
    public string return_type { get; set; }
    
    public int system_priority { get; set; }
}