using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bliss.Core;
using Bliss.ProjectRule.Analyzer;
using Bliss.ProjectRule.SourceGenerator.CodeTargets;
using Bliss.ProjectRule.SourceGenerator.CodeTargets.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Bliss.ProjectRule.SourceGenerator.Generators;

[Generator]
public class EntitySystemGenerator : ISourceGenerator
{
    private static readonly string EntitySystemOfAttributeName = nameof(EntitySystemOfAttribute);

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(()=> SyntaxContextReceiver.Create());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not SyntaxContextReceiver receiver || receiver.ClassDeclarations.Count == 0)
        {
            return;
        }
        
        foreach (var classDeclarationSyntax in receiver.ClassDeclarations)
        {
            GenerateCSFiles(classDeclarationSyntax, ref context);
        }
    }
    
    private void GenerateCSFiles(ClassDeclarationSyntax classDeclarationSyntax,
        ref GeneratorExecutionContext context)
    {
        string className = classDeclarationSyntax.Identifier.Text;
        SemanticModel semanticModel = context.Compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
        INamedTypeSymbol? classTypeSymbol = ModelExtensions.GetDeclaredSymbol(semanticModel, classDeclarationSyntax) as INamedTypeSymbol;
        if (classTypeSymbol == null)
        {
            return;
        }

        if (!classTypeSymbol.IsStatic || !classDeclarationSyntax.IsPartial())
        {
            return;
        }
        
        var entitySystemOfAttributeData =  classTypeSymbol.GetFirstAttribute(EntitySystemOfAttributeName);
        if (entitySystemOfAttributeData == null)
        {
            return;
        }

        if (entitySystemOfAttributeData.ConstructorArguments[0].Value  is not INamedTypeSymbol namedTypeSymbol)
        {
            return;
        }

        var entityClassName = namedTypeSymbol.Name;
        var relevantInterfaces = namedTypeSymbol.GetInterfacesSubOf(nameof(IEntitySystemConstraint));
        
        var defEntitySystems = new List<DefEntitySystem>();
        foreach (var iEntitySystemSymbol in relevantInterfaces)
        {
            var defEntitySystem = new DefEntitySystem();
            
            defEntitySystem.method_name = iEntitySystemSymbol.Name;
            if (defEntitySystem.method_name.StartsWith("I"))
            {
                defEntitySystem.method_name  =  defEntitySystem.method_name.Substring(1);   
            }

            defEntitySystem.generic_types = new List<string>();
            defEntitySystem.generic_types.Add(namedTypeSymbol.GetFullName());
            iEntitySystemSymbol.GetTypeArgumentFullNames(defEntitySystem.generic_types);

            defEntitySystem.return_type = string.Empty;
            
            defEntitySystems.Add(defEntitySystem);
        }
        
        string? namespaceName = classTypeSymbol.GetNameSpace();

        var generationContext = new GenerationContext()
        {
            NameSpace = namespaceName ?? string.Empty,
            ClassName = entityClassName,
        };
        
        var generator = new CSharpTemplateCodeTarget();
        string code = generator.GenerateCode(generationContext, defEntitySystems, Definition.TemplateMap.GetTemplateName(EntitySystemOfAttributeName), ref context);
        string fileName = $"{className}.Generated.cs";
        context.AddSource(fileName, code);

    }
    
    class SyntaxContextReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> ClassDeclarations { get; } = new();

        public static SyntaxContextReceiver Create()
        {
            return new SyntaxContextReceiver();
        }

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is not ClassDeclarationSyntax classDeclarationSyntax)
            {
                return;
            }
            
            if (classDeclarationSyntax.AttributeLists.Count == 0)
            {
                return;
            }

            if (classDeclarationSyntax.Modifiers.IndexOf(SyntaxKind.StaticKeyword) == -1
                || classDeclarationSyntax.Modifiers.IndexOf(SyntaxKind.PartialKeyword) == -1)
            {
                return;
            }

            bool found = false;
            ClassDeclarationSyntax entitySystemOfClass = null;
            foreach (AttributeListSyntax attributeListSyntax in classDeclarationSyntax.AttributeLists)
            {
                AttributeSyntax? attribute = attributeListSyntax.Attributes.FirstOrDefault();
                if (attribute == null)
                {
                    continue;
                }
                
                string attributeClassName = attribute.GetAttributeClassName();
                if (attributeClassName == EntitySystemOfAttributeName)
                {
                    found = true;
                }

                AttributeArgumentSyntax? argumentSyntax =  attribute.ArgumentList?.Arguments.FirstOrDefault();
                if(argumentSyntax == null)
                {
                    continue;
                }
            }

            if (!found)
            {
                return;
            }

            if (ClassDeclarations.Contains(classDeclarationSyntax))
            {
                return;
            }

            ClassDeclarations.Add(classDeclarationSyntax);
        }
    }
}