using System.Collections.Generic;
using System.Collections.Immutable;
using Bliss.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bliss.ProjectRule.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EntitySystemOfAnalyzer : DiagnosticAnalyzer
{
    private const string AttributeName =nameof(EntitySystemOfAttribute);
    private readonly string AttributeFullName = typeof(EntitySystemOfAttribute).FullName;
    
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(EntitySystemOfAnalyzerRule.Rule, EntitySystemMethodAnalyzerRule.Rule);

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
                analysisContext.RegisterSemanticModelAction(AnalyzerClassDeclaration);
            }
        });
    }


    private void AnalyzerClassDeclaration(SemanticModelAnalysisContext context)
    {
        var classDeclarationSyntaxes =  context.SemanticModel.SyntaxTree.GetRoot().DescendantNodes<ClassDeclarationSyntax>();
        foreach (var classDeclarationSyntax in classDeclarationSyntaxes)
        {
            AnalyzerEntitySystemClassDeclaration(context, classDeclarationSyntax);
            AnalyzerEntitySystemListMethodDeclaration(context, classDeclarationSyntax);
        }
    }

    private void AnalyzerEntitySystemClassDeclaration(SemanticModelAnalysisContext context, ClassDeclarationSyntax classDeclarationSyntax)
    {
        ISymbol? classSymbol = ModelExtensions.GetDeclaredSymbol(context.SemanticModel, classDeclarationSyntax);
        if (classSymbol == null)
        {
            return;
        }

        bool foundEntitySystem = false;
        foreach (var attributeData in classSymbol.GetAttributes())
        {
            if (attributeData.AttributeClass?.Name == AttributeName)
            {
                foundEntitySystem = true;
                break;
            }
        }

        if (!foundEntitySystem)
        {
            return;
        }

        if (classSymbol.IsStatic && classDeclarationSyntax.IsPartial())
        {
            return;
        }
        
        var builder = ImmutableDictionary.CreateBuilder<string, string?>();
        builder.Add(AttributeName, classSymbol.Name);
            
        Diagnostic diagnostic = Diagnostic.Create(EntitySystemOfAnalyzerRule.Rule, 
            classDeclarationSyntax.GetLocation(), 
            builder.ToImmutable(), 
            classSymbol.Name);
        context.ReportDiagnostic(diagnostic);
    }

    private void AnalyzerEntitySystemListMethodDeclaration(SemanticModelAnalysisContext context,
        ClassDeclarationSyntax classDeclarationSyntax)
    {
        if (classDeclarationSyntax.Kind() == SyntaxKind.InterfaceDeclaration)
        {
            return;
        }
        
        ISymbol? classSymbol = ModelExtensions.GetDeclaredSymbol(context.SemanticModel, classDeclarationSyntax);
        if (classSymbol == null
            || !classSymbol.IsStatic
            || !classDeclarationSyntax.IsPartial()
            || classSymbol is not INamedTypeSymbol classTypeSymbol)
        {
            return;
        }
        
        var entitySystemOfAttributeData = classTypeSymbol.GetFirstAttribute(AttributeFullName);
        if (entitySystemOfAttributeData == null)
        {
            return;
        }

        if (entitySystemOfAttributeData.ConstructorArguments[0].Value  is not INamedTypeSymbol namedTypeSymbol)
        {
            return;
        }

        var relevantInterfaces = namedTypeSymbol.GetInterfacesSubOf(nameof(IEntitySystemConstraint));
        
        HashSet<string> candidateDeclaredMethod = new HashSet<string>();
        List<string> paramNames = new List<string>();
        
        foreach (var memberSymbol in classTypeSymbol.GetMembers())
        {
            if (memberSymbol is not IMethodSymbol methodSymbol)
            {
                continue;
            }

            if (methodSymbol.DeclaredAccessibility != Accessibility.Public
                || !methodSymbol.IsStatic
                || !methodSymbol.ReturnsVoid)
            {
                continue;
            }

            paramNames.Clear();
            methodSymbol.GetParamFullNames(paramNames);
            string paramTypes = string.Join(", ", paramNames);
            
            candidateDeclaredMethod.Add($"public static void {methodSymbol.Name}({paramTypes})");
        }

        List<string> typeArgumentNames = new List<string>();
        foreach (var iEntitySystemSymbol in relevantInterfaces)
        {
            string methodName = iEntitySystemSymbol.GetMethodNameByInterface();
            
            typeArgumentNames.Clear();
            
            typeArgumentNames.Add($"this {namedTypeSymbol.ToDisplayString()}");
            iEntitySystemSymbol.GetTypeArgumentFullNames(typeArgumentNames);
            string paramTypes = string.Join(", ", typeArgumentNames);
            
            string methodSignature = $"public static void {methodName}({paramTypes})";

            if (!candidateDeclaredMethod.Contains(methodSignature))
            {
                var builder = ImmutableDictionary.CreateBuilder<string, string?>();
                builder.Add(AttributeName, classSymbol.Name);
                builder.Add(Definition.DiagnosticProperty.MethodName, methodName);
                
                typeArgumentNames.Clear();
                typeArgumentNames.Add($"this {namedTypeSymbol.GetName()}");
                iEntitySystemSymbol.GetTypeArgumentNames(typeArgumentNames);
                paramTypes = string.Join(", ", typeArgumentNames);
                
                builder.Add(Definition.DiagnosticProperty.ParamTypes, paramTypes);
            
                Diagnostic diagnostic = Diagnostic.Create(EntitySystemMethodAnalyzerRule.Rule, 
                    classDeclarationSyntax.GetLocation(), 
                    builder.ToImmutable(), 
                    classSymbol.Name, methodSignature);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}