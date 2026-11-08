using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bliss.ProjectRule.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EntityUpdateRegistrationAnalyzer : DiagnosticAnalyzer
{
    private const string ENTITY_SYSTEM_TYPE_NAME = "Bliss.EntitySystem";
    private const string ENTITY_TYPE_NAME = "Bliss.Entity";
    private const string GAMEPLAY_ENTITY_TYPE_NAME = "Bliss.GamePlayEntity";
    private const string GAMEPLAY_WORLD_TYPE_NAME = "Bliss.GameplayWorld";
    private const string GAMEPLAY_WORLD_UPDATER_TYPE_NAME = "Bliss.GameplayWorldUpdater";
    private const string REGISTER_SYSTEM_METHOD_NAME = "RegisterSystem";
    private const string REGISTER_TO_SYSTEM_METHOD_NAME = "RegisterToSystem";
    private const string REGISTER_METHOD_NAME = "Register";
    private const string ADD_METHOD_NAME = "Add";
    private const string LOGIC_SCENE_PROPERTY_NAME = "LogicScene";
    private const string UNIT_TEST_ASSEMBLY_NAME = "Bliss.UnitTest";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(EntityUpdateRegistrationAnalyzerRule.Rule);

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
            if (analysisContext.Compilation.AssemblyName != null
                && analysisContext.Compilation.AssemblyName != UNIT_TEST_ASSEMBLY_NAME
                && AnalyzerHelper.IsAssemblyNeedAnalyze(analysisContext.Compilation.AssemblyName, AnalyzeAssembly.All))
            {
                analysisContext.RegisterSyntaxNodeAction(analyzeInvocation, SyntaxKind.InvocationExpression);
            }
        });
    }

    private static void analyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocationExpression)
        {
            return;
        }

        if (context.SemanticModel.GetSymbolInfo(invocationExpression, context.CancellationToken).Symbol is not IMethodSymbol targetMethod
            || !isRegistrationMethod(targetMethod))
        {
            return;
        }

        if (context.SemanticModel.GetEnclosingSymbol(invocationExpression.SpanStart, context.CancellationToken) is IMethodSymbol callerMethod
            && isApprovedCall(targetMethod, callerMethod, invocationExpression))
        {
            return;
        }

        Diagnostic diagnostic = Diagnostic.Create(
            EntityUpdateRegistrationAnalyzerRule.Rule,
            invocationExpression.Expression.GetLocation(),
            targetMethod.Name);
        context.ReportDiagnostic(diagnostic);
    }

    private static bool isRegistrationMethod(IMethodSymbol methodSymbol)
    {
        string containingTypeName = methodSymbol.ContainingType.ToDisplayString();
        return (methodSymbol.Name == REGISTER_SYSTEM_METHOD_NAME && isEntitySystemRegisterMethod(methodSymbol))
            || (methodSymbol.Name == REGISTER_TO_SYSTEM_METHOD_NAME && isEntityRegisterMethod(methodSymbol))
            || (methodSymbol.Name == REGISTER_METHOD_NAME && containingTypeName == GAMEPLAY_WORLD_TYPE_NAME)
            || (methodSymbol.Name == ADD_METHOD_NAME && containingTypeName == GAMEPLAY_WORLD_UPDATER_TYPE_NAME);
    }

    private static bool isApprovedCall(
        IMethodSymbol targetMethod,
        IMethodSymbol callerMethod,
        InvocationExpressionSyntax invocationExpression)
    {
        string targetTypeName = targetMethod.ContainingType.ToDisplayString();
        string callerTypeName = callerMethod.ContainingType.ToDisplayString();

        if (isEntitySystemRegisterMethod(targetMethod))
        {
            return targetTypeName == ENTITY_SYSTEM_TYPE_NAME
                && callerMethod.Name == REGISTER_TO_SYSTEM_METHOD_NAME
                && callerTypeName == ENTITY_TYPE_NAME
                && callerMethod.Parameters.Length == 0
                && callerMethod.ReturnsVoid
                && callerMethod.IsVirtual;
        }

        if (targetMethod.Name == REGISTER_TO_SYSTEM_METHOD_NAME && isEntityRegisterMethod(targetMethod))
        {
            return (isLogicSceneSetter(callerMethod) && isCurrentInstanceInvocation(invocationExpression))
                || (isEntityRegisterMethod(callerMethod) && isBaseInvocation(invocationExpression));
        }

        if (targetMethod.Name == REGISTER_METHOD_NAME && targetTypeName == GAMEPLAY_WORLD_TYPE_NAME)
        {
            return callerMethod.Name == REGISTER_TO_SYSTEM_METHOD_NAME
                && callerTypeName == GAMEPLAY_ENTITY_TYPE_NAME
                && isEntityRegisterMethod(callerMethod);
        }

        if (targetMethod.Name == ADD_METHOD_NAME && targetTypeName == GAMEPLAY_WORLD_UPDATER_TYPE_NAME)
        {
            return callerMethod.Name == REGISTER_METHOD_NAME
                && callerTypeName == GAMEPLAY_WORLD_TYPE_NAME
                && callerMethod.Parameters.Length == 1
                && callerMethod.Parameters[0].Type.ToDisplayString() == ENTITY_TYPE_NAME
                && callerMethod.ReturnsVoid;
        }

        return false;
    }

    private static bool isEntityRegisterMethod(IMethodSymbol methodSymbol)
    {
        IMethodSymbol? currentMethod = methodSymbol;
        while (currentMethod != null)
        {
            if (currentMethod.Name == REGISTER_TO_SYSTEM_METHOD_NAME
                && currentMethod.ContainingType.ToDisplayString() == ENTITY_TYPE_NAME
                && currentMethod.Parameters.Length == 0
                && currentMethod.ReturnsVoid
                && currentMethod.DeclaredAccessibility == Accessibility.Protected
                && (currentMethod.IsVirtual || currentMethod.IsOverride))
            {
                return true;
            }

            currentMethod = currentMethod.OverriddenMethod;
        }

        return false;
    }

    private static bool isEntitySystemRegisterMethod(IMethodSymbol methodSymbol)
    {
        IMethodSymbol? currentMethod = methodSymbol;
        while (currentMethod != null)
        {
            if (currentMethod.Name == REGISTER_SYSTEM_METHOD_NAME
                && currentMethod.ContainingType.ToDisplayString() == ENTITY_SYSTEM_TYPE_NAME
                && currentMethod.Parameters.Length == 1
                && currentMethod.Parameters[0].Type.ToDisplayString() == ENTITY_TYPE_NAME
                && currentMethod.ReturnsVoid
                && (currentMethod.IsVirtual || currentMethod.IsOverride))
            {
                return true;
            }

            currentMethod = currentMethod.OverriddenMethod;
        }

        return false;
    }

    private static bool isLogicSceneSetter(IMethodSymbol methodSymbol)
    {
        return methodSymbol.MethodKind == MethodKind.PropertySet
            && methodSymbol.AssociatedSymbol is IPropertySymbol propertySymbol
            && propertySymbol.Name == LOGIC_SCENE_PROPERTY_NAME
            && propertySymbol.ContainingType.ToDisplayString() == ENTITY_TYPE_NAME;
    }

    private static bool isBaseInvocation(InvocationExpressionSyntax invocationExpression)
    {
        return invocationExpression.Expression is MemberAccessExpressionSyntax memberAccessExpression
            && memberAccessExpression.Expression is BaseExpressionSyntax;
    }

    private static bool isCurrentInstanceInvocation(InvocationExpressionSyntax invocationExpression)
    {
        return invocationExpression.Expression is IdentifierNameSyntax
            || (invocationExpression.Expression is MemberAccessExpressionSyntax memberAccessExpression
                && memberAccessExpression.Expression is ThisExpressionSyntax);
    }
}
