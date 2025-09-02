using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bliss.ProjectRule.Analyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Formatting;

namespace Bliss.ProjectRule.CodeFix;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EntitySystemMethodCodeFixProvider)), Shared]
public class EntitySystemMethodCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIds.EntitySystemMethodRuleId);

    public override FixAllProvider? GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            
        Diagnostic diagnostic = context.Diagnostics.First();

        Microsoft.CodeAnalysis.Text.TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;
        
        ClassDeclarationSyntax? classDeclaration = root?.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().First();
        
        CodeAction codeAction = CodeAction.Create(
            "Generate Entity System",
            cancelToken => GenerateEntitySystemAsync(context.Document,classDeclaration,diagnostic,cancelToken), 
            equivalenceKey: nameof(EntitySystemMethodCodeFixProvider));
        context.RegisterCodeFix(codeAction,diagnostic);
    }

    private static async Task<Document> GenerateEntitySystemAsync(Document document,
        ClassDeclarationSyntax? classDeclaration, Diagnostic diagnostic,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        
        SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        
        if (classDeclaration == null || root == null)
        {
            return document;
        }
        
        ImmutableDictionary<string, string?> properties = diagnostic.Properties;
        string? methodName = properties[Definition.DiagnosticProperty.MethodName];
        string? paramTypes = properties[Definition.DiagnosticProperty.ParamTypes];

        if (methodName == null || paramTypes == null)
        {
            return document;
        }
        
        StringBuilder sb =  new StringBuilder();
        string[] paramTypeArray = paramTypes.Split([", "], StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < paramTypeArray.Length; i++)
        {
            var paramType = paramTypeArray[i];
            if (paramType.StartsWith("this"))
            {
                sb.Append($"{paramType} self");
            }
            else
            {
                sb.Append($"{paramType} param{i}");
            }

            if (i != paramTypeArray.Length - 1)
            {
                sb.Append(", ");
            }
        }

        string paramsDefine = sb.ToString();

        string sourceCode = $$"""
                               public static void {{methodName}}({{paramsDefine}}) 
                               {
                               
                               }
                              """;

        var newMethodSyntax = SyntaxFactory.ParseMemberDeclaration(sourceCode) as MethodDeclarationSyntax;
        if (newMethodSyntax == null)
        {
            return document;
        }
            
        var newClassDeclaration = classDeclaration.WithMembers(classDeclaration.Members.Add(newMethodSyntax)).WithAdditionalAnnotations(Formatter.Annotation);
        document = document.WithSyntaxRoot(root.ReplaceNode(classDeclaration, newClassDeclaration));
        document = await CleanupDocumentAsync(document, cancellationToken);
        return document;
    }

    
    internal static async Task<Document> CleanupDocumentAsync(
        Document document, CancellationToken cancellationToken)
    {
        if (document.SupportsSyntaxTree)
        {
            document = await ImportAdder.AddImportsAsync(
                document, Simplifier.AddImportsAnnotation, cancellationToken: cancellationToken).ConfigureAwait(false);

            document = await Simplifier.ReduceAsync(document, Simplifier.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);

            // format any node with explicit formatter annotation
            document = await Formatter.FormatAsync(document, Formatter.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);

            // format any elastic whitespace
            document = await Formatter.FormatAsync(document, SyntaxAnnotation.ElasticAnnotation, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        return document;
    }
}