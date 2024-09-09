using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Async.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TaskResultAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "TaskResultAnalyzer";

        private static readonly LocalizableString Title = "Avoid direct 'Result' usage on Task";
        private static readonly LocalizableString MessageFormat = "Consider using 'await' to access the result of the Task";
        private static readonly LocalizableString Description = "Using 'Result' can lead to deadlocks and threadpool exhaustion. Prefer 'await'.";
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
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.SimpleMemberAccessExpression);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var memberAccessExpr = (MemberAccessExpressionSyntax)context.Node;
            var memberSymbol = context.SemanticModel.GetSymbolInfo(memberAccessExpr).Symbol;

            if (memberSymbol == null)
                return;
            if (memberSymbol.ContainingType == null)
                return;
            if (!memberSymbol.ContainingType.IsTaskType(context.SemanticModel))
                return;

            if (memberAccessExpr.Name == null)
                return;
            if (memberAccessExpr.Name.Identifier.Text == "Result")
            {
                var diagnostic = Diagnostic.Create(Rule, memberAccessExpr.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}