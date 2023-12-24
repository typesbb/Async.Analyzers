using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TaskAwaiterResultAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TaskAwaiterResultAnalyzer";

    private static readonly LocalizableString Title = "Avoid direct 'GetAwaiter().GetResult()' usage on Task";
    private static readonly LocalizableString MessageFormat = "Consider using 'await' to access the result of the Task";
    private static readonly LocalizableString Description = "Using 'GetAwaiter().GetResult()' can lead to deadlocks and threadpool exhaustion. Prefer 'await'.";
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
        var memberAccessExpr = invocationExpr.Expression as MemberAccessExpressionSyntax;
        if (memberAccessExpr == null)
            return;

        if (memberAccessExpr.Name.Identifier.Text == "GetResult" &&
            memberAccessExpr.Expression is InvocationExpressionSyntax invocation && invocation != null &&
            invocation.Expression is MemberAccessExpressionSyntax accessor && accessor != null &&
            accessor.Name.Identifier.Text == "GetAwaiter")
        {
            var memberSymbol = context.SemanticModel.GetSymbolInfo(accessor).Symbol;
            if (memberSymbol == null)
                return;
            if (!memberSymbol.ContainingType.IsTaskType(context.SemanticModel))
                return;
            var diagnostic = Diagnostic.Create(Rule, invocationExpr.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
