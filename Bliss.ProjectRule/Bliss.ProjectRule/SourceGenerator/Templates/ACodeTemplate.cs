using System.Collections.Generic;
using Scriban;

namespace Bliss.ProjectRule.SourceGenerator.Templates;

internal abstract class ACodeTemplate
{
    public abstract (string, Template) Get();
}