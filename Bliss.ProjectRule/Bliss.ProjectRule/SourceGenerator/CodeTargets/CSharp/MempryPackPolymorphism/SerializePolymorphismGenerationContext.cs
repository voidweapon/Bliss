using System.Collections.Generic;

namespace Bliss.ProjectRule.SourceGenerator.CodeTargets.CSharp;

public class SerializePolymorphismGenerationContext : GenerationContext
{
    public List<string> ClassNames { get; } = new List<string>();
}