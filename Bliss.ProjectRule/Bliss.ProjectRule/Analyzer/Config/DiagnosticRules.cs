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
}