using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bliss.ProjectRule.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DiableNewAnalyzer : DiagnosticAnalyzer
    {
        private const string AttributeName = nameof(DisableNewAttribute);
        
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>ImmutableArray.Create(DisableNewAnalyzerRule.Rule);
        
        public override void Initialize(AnalysisContext context)
        {
            if (!AnalyzerGlobalSetting.EnableAnalyzer)
            {
                return;
            }
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            
            context.RegisterSyntaxNodeAction(this.AnalyzeObjectCreationExpression, SyntaxKind.ObjectCreationExpression);
        }

        private void AnalyzeObjectCreationExpression(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not ObjectCreationExpressionSyntax objectCreationExpressionSyntax)
            {
                return;
            }
            
            var typeSymbol = context.SemanticModel.GetSymbolInfo(objectCreationExpressionSyntax.Type).Symbol as ITypeSymbol;
            if (typeSymbol==null)
            {
                return;
            }
            if (typeSymbol.HasAttributeInTypeAndBaseTypes(AttributeName))
            {
                Diagnostic diagnostic = Diagnostic.Create(DisableNewAnalyzerRule.Rule, objectCreationExpressionSyntax?.GetLocation(),typeSymbol);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}