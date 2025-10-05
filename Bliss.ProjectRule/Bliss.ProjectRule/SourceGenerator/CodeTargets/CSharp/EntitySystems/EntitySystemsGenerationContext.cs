using System.Collections.Generic;
using Bliss.ProjectRule.SourceGenerator.Generators;

namespace Bliss.ProjectRule.SourceGenerator.CodeTargets.CSharp;

public class EntitySystemsGenerationContext : GenerationContext
{
    public List<DefEntitySystem> defEntitySystems = new List<DefEntitySystem>();
}