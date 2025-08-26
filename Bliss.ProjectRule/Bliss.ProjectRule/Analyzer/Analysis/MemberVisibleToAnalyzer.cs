using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bliss.ProjectRule.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MemberVisibleToAnalyzer : DiagnosticAnalyzer
    {
        private const string AttributeName = "MemberVisibleToAttribute";
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(MemberVisibleToAnalyzerRule.Rule);
        
        public override void Initialize(AnalysisContext context)
        {
            if (!AnalyzerGlobalSetting.EnableAnalyzer)
            {
                return;
            }
            
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(analysisContext =>
            {
                if (analysisContext.Compilation.AssemblyName != null && AnalyzerHelper.IsAssemblyNeedAnalyze(analysisContext.Compilation.AssemblyName, AnalyzeAssembly.All))
                {
                    analysisContext.RegisterSemanticModelAction(AnalyzePropertyAccessPermission);
                }
            });
        }

        private void AnalyzePropertyAccessPermission(SemanticModelAnalysisContext analysisContext)
        {
            var assignmentExpressionSyntaxes = analysisContext.SemanticModel.SyntaxTree.GetRoot() .DescendantNodes<AssignmentExpressionSyntax>();
            var memberAccessExpressionSyntaxes = analysisContext.SemanticModel.SyntaxTree.GetRoot() .DescendantNodes<MemberAccessExpressionSyntax>();
            foreach (var memberAccessExpressionSyntax in analysisContext.SemanticModel.SyntaxTree.GetRoot().DescendantNodes<MemberAccessExpressionSyntax>())
            {
                AnalyzeMemberAccessExpression(analysisContext, memberAccessExpressionSyntax);
            }
            
            //Other Class Get
                //SimpleMemberAccessExpression
            //Other Class Set
                //SimpleAssignmentExpression->SimpleMemberAccessExpression
            //Self Class Get
            //Self Class Set
                //SimpleAssignmentExpression
        }

        private void AnalyzeMemberAccessExpression(SemanticModelAnalysisContext context, MemberAccessExpressionSyntax memberAccessExpressionSyntax)
        {
            ISymbol? memberSymbol = context.SemanticModel.GetSymbolInfo(memberAccessExpressionSyntax, context.CancellationToken).Symbol;
            if (memberSymbol == null || (!(memberSymbol is IPropertySymbol) && !(memberSymbol is IFieldSymbol)))
            {
                return;
            }
            
            ClassDeclarationSyntax? accessFieldClassDeclaration = memberAccessExpressionSyntax.GetParentClassDeclaration();
            if (accessFieldClassDeclaration == null)
            {
                return;
            }

            INamedTypeSymbol? accessFieldClassSymbol = context.SemanticModel.GetDeclaredSymbol(accessFieldClassDeclaration);

            if (accessFieldClassSymbol == null)
            {
                return;
            }

            bool isPass = false;
            if(memberSymbol is IPropertySymbol propertySymbol)
            {
                isPass = IsVisible(propertySymbol, accessFieldClassSymbol, memberAccessExpressionSyntax);
            }
            else if(memberSymbol is IFieldSymbol fieldSymbol)
            {
                isPass = IsVisible(fieldSymbol, accessFieldClassSymbol, memberAccessExpressionSyntax);
            }

            if (isPass)
            {
                return;
            }
            
            var builder = ImmutableDictionary.CreateBuilder<string, string?>();
            builder.Add(AttributeName, memberSymbol.ContainingType.ToString());
            
            Diagnostic diagnostic = Diagnostic.Create(MemberVisibleToAnalyzerRule.Rule, 
                memberAccessExpressionSyntax.GetLocation(), 
                builder.ToImmutable(), 
                memberSymbol.ContainingType.ToString(), memberSymbol.Name);
            context.ReportDiagnostic(diagnostic);
        }

        private bool IsVisible(ISymbol memberSymbol, ITypeSymbol accessMemberClassSymbol, MemberAccessExpressionSyntax memberAccessExpressionSyntax)
        {
            ImmutableArray<AttributeData> memberAttributes  = memberSymbol.GetAttributes();
            bool isPass = false;
            bool haveTargetAttribute = false;
            foreach (var attributeData in memberAttributes)
            {
                if (attributeData.AttributeClass?.Name != AttributeName)
                {
                    continue;
                }

                if (attributeData.ConstructorArguments.Length != 2)
                {
                    continue;
                }
                
                if (attributeData.ConstructorArguments[1].Kind != TypedConstantKind.Enum)
                {
                    continue;
                }

                int accessMode = (int)attributeData.ConstructorArguments[1].Value;
                if ((accessMode & 3) != 3)
                {
                    if ((accessMode & 3) == 1)
                    {
                        //Get
                        if (!(!memberAccessExpressionSyntax.HasLeadingTrivia && !memberAccessExpressionSyntax.HasTrailingTrivia))
                        {
                            continue;
                        }
                    }
                    
                    if ((accessMode & 3) == 2)
                    {
                        //Set
                        if (!(memberAccessExpressionSyntax.HasLeadingTrivia /*&& memberAccessExpressionSyntax.HasTrailingTrivia*/))
                        {
                            continue;
                        }
                    }
                }
                
                haveTargetAttribute = true;
                    
                if (!(attributeData.ConstructorArguments[0].Value is INamedTypeSymbol namedTypeSymbol))
                {
                    continue;
                }
                
                if (namedTypeSymbol.ToString() == accessMemberClassSymbol.ToString())
                {
                    isPass = true;
                }
            }

            if (!haveTargetAttribute)
            {
                return true;
            }
            
            return isPass;
        }
    }
}