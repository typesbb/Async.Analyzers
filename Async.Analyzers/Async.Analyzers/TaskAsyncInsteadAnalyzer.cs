using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TaskAsyncInsteadAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TaskAsyncInsteadAnalyzer";

    private static readonly LocalizableString Title = "Replace with async method";
    private static readonly LocalizableString MessageFormat = "Consider making method '{0}Async'";
    private static readonly LocalizableString Description = "This analyzer detects synchronous methods that could be made async.";
    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        var invocationExpr = (InvocationExpressionSyntax)context.Node;
        IMethodSymbol methodSymbol = context.SemanticModel.GetSymbolInfo(invocationExpr).Symbol as IMethodSymbol;
        if (methodSymbol == null)
        {
            return;
        }
        if (methodSymbol.ReturnType is INamedTypeSymbol typeSymbol1 && typeSymbol1 != null && typeSymbol1.IsTaskType(context.SemanticModel))
        {
            // Skip if the method is already async
            return;
        }

        if (methodSymbol != null)
        {
            // Find async methods with same name as current method
            var asyncMethod = methodSymbol.ContainingType.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => m.ReturnType is INamedTypeSymbol typeSymbol && typeSymbol != null && typeSymbol.IsTaskType(context.SemanticModel) && (m.Name == methodSymbol.Name || m.Name == methodSymbol.Name + "Async"))
                .Where(m => Enumerable.SequenceEqual(m.Parameters, methodSymbol.Parameters, SymbolEqualityComparer.Default))
                .FirstOrDefault();
            if (asyncMethod != null)
            {
                Diagnostic diagnostic = Diagnostic.Create(Rule, invocationExpr.GetLocation(), methodSymbol.Name, asyncMethod.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
