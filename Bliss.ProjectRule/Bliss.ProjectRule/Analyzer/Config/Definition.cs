using System.Collections.Generic;
using Bliss.Core;

namespace Bliss.ProjectRule.Analyzer;

public  static class Definition
{
    public static class DiagnosticProperty
    {
        public static readonly string MethodName = "MethodName";
        public static readonly string ClassName = "ClassName"; 
        public static readonly string ParamTypes = "ParamTypes"; 
    }

    public static class TemplateMap
    {
        public static readonly string EntitySystem = "EntitySystem"; 
        
        /// <summary>
        /// Attribute Class Name -> Template Name
        /// </summary>
        private static readonly Dictionary<string, string> _entitySystemMap = new Dictionary<string, string>()
        {
            {nameof(EntitySystemOfAttribute), EntitySystem},
        };

        public static string GetTemplateName(string entitySystemAttribute)
        {
            return _entitySystemMap[entitySystemAttribute];
        }
    }
}