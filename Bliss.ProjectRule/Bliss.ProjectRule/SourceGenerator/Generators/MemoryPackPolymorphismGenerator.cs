using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Bliss.ProjectRule.Analyzer;
using Bliss.ProjectRule.SourceGenerator.CodeTargets.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bliss.ProjectRule.SourceGenerator.Generators;

[Generator]
public class MemoryPackPolymorphismGenerator : ISourceGenerator
{
    private const string PolymorphismAttributeName = nameof(SerializePolymorphismAttribute);
    private const string PolymorphismRegisterAttributeName = nameof(SerializePolymorphismRegisterAttribute);
    private const string MemoryPackableAttributeName = "MemoryPack.MemoryPackableAttribute";
    
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(()=> MemoryPackPolymorphismSyntaxContextReceiver.Create());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not MemoryPackPolymorphismSyntaxContextReceiver receiver || receiver.CandicateClassDeclaration.Count == 0)
        {
            return;
        }

        //构建继承树
        Dictionary<ITypeSymbol, HashSet<ITypeSymbol>> inheritanceGroup = new();

        foreach (var candicateClasseclaration in receiver.CandicateClassDeclaration)
        {
            SemanticModel semanticModel = context.Compilation.GetSemanticModel(candicateClasseclaration.SyntaxTree);

            if (!AnalyzerHelper.IsAssemblyNeedAnalyze(semanticModel.Compilation.AssemblyName, AnalyzeAssembly.AllModel))
            {
                continue;   
            }
            
            INamedTypeSymbol? classTypeSymbol = ModelExtensions.GetDeclaredSymbol(semanticModel, candicateClasseclaration) as INamedTypeSymbol;
            if (classTypeSymbol == null)
            {
                continue;
            }
            
            if (classTypeSymbol.TryGetAttributeInDeepestBassType(MemoryPackableAttributeName, out var deepestBassType))
            {
                if (!inheritanceGroup.TryGetValue(deepestBassType, out var group))
                {
                    group = new HashSet<ITypeSymbol>();
                    inheritanceGroup.Add(deepestBassType, group);
                }

                group.Add(classTypeSymbol);
            }
        }
        
        //为每个继承树构建序列化Formater
        foreach (var kv in inheritanceGroup)
        {
            GeneratePolymorphism(context,kv.Key, kv.Value);
        }
        
        //集合各个多态序列化Formater到一个注册器
        if(receiver.RegisterClassDeclaration == null)
        {
            return;
        }
        SemanticModel registerSemanticModel = context.Compilation.GetSemanticModel(receiver.RegisterClassDeclaration.SyntaxTree);
        INamedTypeSymbol? registerclassTypeSymbol = ModelExtensions.GetDeclaredSymbol(registerSemanticModel, receiver.RegisterClassDeclaration) as INamedTypeSymbol;

        if (registerclassTypeSymbol == null)
        {
            return;
        }
        GeneratePolymorphismRegister(context, registerclassTypeSymbol, inheritanceGroup.Keys.ToList());
    }

    private void GeneratePolymorphism(GeneratorExecutionContext context, 
        ITypeSymbol baseClassTypeSymbol, 
        HashSet<ITypeSymbol> subclassTypeSymbolSet)
    {
        var className = baseClassTypeSymbol.ToDisplayString();

        bool isDotNet = false;
        if (context.Compilation.AssemblyName != null)
        {
            isDotNet = !context.Compilation.AssemblyName.StartsWith("Bliss");
        }

        var generationContext = new SerializePolymorphismGenerationContext()
        {
            NameSpace = string.Empty,
            ClassName = className,
            IsDotNet = isDotNet,
        };
        
        foreach (var subClassTypeSymbol in subclassTypeSymbolSet)
        {
            generationContext.ClassNames.Add(subClassTypeSymbol.ToDisplayString());
        }
        
        var generator = new CSharpMemoryPackPolymorphismGeneratorCodeTarget();
        string code = generator.GenerateCode(generationContext, Definition.TemplateMap.GetTemplateName(PolymorphismAttributeName), ref context);
        string fileName = $"{className}SerializeFormatter.Generated.cs";
        context.AddSource(fileName, code);
    }

    private void GeneratePolymorphismRegister(GeneratorExecutionContext context, 
        ITypeSymbol registerClassTypeSymbol, 
        List<ITypeSymbol>  baseclassTypeSymbolSet)
    {
        var className = registerClassTypeSymbol.ToDisplayString();
        var namespaceName = registerClassTypeSymbol.GetNameSpace();
        
        bool isDotNet = false;
        if (context.Compilation.AssemblyName != null)
        {
            isDotNet = !context.Compilation.AssemblyName.StartsWith("Unity");
        }

        var generationContext = new SerializePolymorphismGenerationContext()
        {
            NameSpace = namespaceName,
            ClassName = registerClassTypeSymbol.Name,
            IsDotNet = isDotNet,
        };
        
        foreach (var baseClassTypeSymbol in baseclassTypeSymbolSet)
        {
            generationContext.ClassNames.Add(baseClassTypeSymbol.ToDisplayString());
        }
        
        var generator = new CSharpMemoryPackPolymorphismGeneratorCodeTarget();
        string code = generator.GenerateCode(generationContext, Definition.TemplateMap.GetTemplateName(PolymorphismRegisterAttributeName), ref context);
        string fileName = $"{className}.Generated.cs";
        context.AddSource(fileName, code);
    }

    class MemoryPackPolymorphismSyntaxContextReceiver : ISyntaxReceiver
    {
        public ClassDeclarationSyntax RegisterClassDeclaration;
        public readonly List<ClassDeclarationSyntax> CandicateClassDeclaration = new();
        public static MemoryPackPolymorphismSyntaxContextReceiver Create()
        {
            return new MemoryPackPolymorphismSyntaxContextReceiver();
        }
        
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is not ClassDeclarationSyntax classDeclaration)
            {
                return;
            }
            
            if(classDeclaration.Modifiers.IndexOf(SyntaxKind.PartialKeyword) == -1)
            {
                return;
            }

            if (classDeclaration.Modifiers.IndexOf(SyntaxKind.StaticKeyword) != -1
                && classDeclaration.HasAttributeDeclaredOnly(PolymorphismRegisterAttributeName))
            {
                RegisterClassDeclaration = classDeclaration;
            }
            
            if (!classDeclaration.HasAttributeDeclaredOnly(PolymorphismAttributeName)
                || !classDeclaration.HasAttributeDeclaredOnly(MemoryPackableAttributeName))
            {
                return;
            }
            
            CandicateClassDeclaration.Add(classDeclaration);
        }
    }
}