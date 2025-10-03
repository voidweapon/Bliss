using Microsoft.CodeAnalysis;

namespace Bliss.ProjectRule.Analyzer
{
    public static class MemberVisibleToAnalyzerRule
    {
        private const string Title = "属性可见性限制限制";

        private const string MessageFormat = "Type: {0} 不允许作为实体: {1} 的AddChild函数参数类型! 若要允许该类型作为参数,请使用ChildOfAttribute对child实体类标记父级类型";

        private const string Description = "该属性只能由指定的类型访问.";

        public static readonly DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(DiagnosticIds.MemberVisibleToRuleId,
                Title,
                MessageFormat,
                DiagnosticCategories.Visibility,
                DiagnosticSeverity.Error,
                true,
                Description);
    }
    
    public static class EntitySystemOfAnalyzerRule
    {
        private const string Title = "EntitySystemOf标签只能添加到分部静态类上";

        private const string MessageFormat = "实体逻辑类: {0} 必须是 static partial class";

        private const string Description = "EntitySystemOf标签只能添加到分部静态类上.";

        public static readonly DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(DiagnosticIds.EntitySystemOfRuleId,
                Title,
                MessageFormat,
                DiagnosticCategories.SourceGenerate,
                DiagnosticSeverity.Error,
                true,
                Description);
    }
    
    
    public static class EntitySystemMethodAnalyzerRule
    {
        private const string Title = "EntitySystem缺少生命周期函数";

        private const string MessageFormat = "实体逻辑类: {0} 缺少生命周期函数: {1}";

        private const string Description = "EntitySystem缺少生命周期函数.";

        public static readonly DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(DiagnosticIds.EntitySystemMethodRuleId,
                Title,
                MessageFormat,
                DiagnosticCategories.EntitySystem,
                DiagnosticSeverity.Error,
                true,
                Description);
    }
    
    public static class DisableNewAnalyzerRule
    {
        private const string Title = "含有DisableNew标记的类禁止使用new构造对象";

        private const string MessageFormat = "禁止使用new构造{0}类型的对象";

        private const string Description = "含有DisableNew标记的类禁止使用new构造对象.";

        public static readonly DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(DiagnosticIds.DisableNewAnalyzerRuleId,
                Title,
                MessageFormat,
                DiagnosticCategories.Project,
                DiagnosticSeverity.Error,
                true,
                Description);
    }
}