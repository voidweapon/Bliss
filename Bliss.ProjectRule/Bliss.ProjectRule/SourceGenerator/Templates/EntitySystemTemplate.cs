using Bliss.ProjectRule.Analyzer;
using Scriban;

namespace Bliss.ProjectRule.SourceGenerator.Templates;

internal class EntitySystemTemplate : ACodeTemplate
{
    private readonly string Templatename = Definition.TemplateMap.EntitySystem;
    private readonly string TemplateString = @"
{{namespace_with_grace_begin __namespace__}}
    public static partial class {{__system_class_name__}}
    {
        {{~for entity_system in __entity_systems__ ~}}
        public class {{__class_name__}}_{{entity_system.method_name}}System{{generic_define_name entity_system.generic_types 1}} : {{entity_system.method_name}}System{{generic_define entity_system.generic_types}}
        {
            public override int GetPriority()
            {
                return {{entity_system.system_priority}};
            }

            protected override {{return_type  entity_system.return_type}} {{entity_system.method_name}}({{generic_params_define entity_system.generic_types}})
            {
                {{return_operate entity_system.return_type}} {{generic_param_name entity_system.generic_types 0}}.{{entity_system.method_name}}({{generic_params_name entity_system.generic_types 1}});
            }
        }
        {{~end~}}

    }

{{namespace_with_grace_end __namespace__}}
";
    
    
    public override (string, Template) Get()
    {
        var template = Template.Parse(this.TemplateString);
        return (Templatename, template);
    }
}