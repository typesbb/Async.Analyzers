using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

namespace Async.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TaskWaitCodeFixProvider)), Shared]
    public class TaskWaitCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(TaskWaitAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var invocationExpr = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf()
                .OfType<InvocationExpressionSyntax>()
                .Where(e => e.Expression is MemberAccessExpressionSyntax accessor && accessor != null && accessor.Name.Identifier.Text == "Wait")
                .First();

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Use 'await'",
                    createChangedDocument: c => ReplaceWithAwaitAsync(context.Document, invocationExpr, c),
                    equivalenceKey: "Use 'await'"),
                diagnostic);
        }

        private async Task<Document> ReplaceWithAwaitAsync(Document document, InvocationExpressionSyntax invocationExpr, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            var memberAccessExpr = invocationExpr.Expression as MemberAccessExpressionSyntax;
            var newExpression = memberAccessExpr.Expression;

            ExpressionSyntax awaitExpression = SyntaxFactory.AwaitExpression(newExpression).WithLeadingTrivia(invocationExpr.GetLeadingTrivia()).WithTrailingTrivia(invocationExpr.GetTrailingTrivia());

            if (invocationExpr.Parent is ExpressionSyntax expressionSyntax && expressionSyntax != null && expressionSyntax.IsPrecedenceGreaterThanAwait())
            {
                awaitExpression = SyntaxFactory.ParenthesizedExpression(awaitExpression)
                    .WithAdditionalAnnotations(Formatter.Annotation);
            }

            editor.ReplaceNode(invocationExpr, awaitExpression);
            return editor.GetChangedDocument();
        }
    }
}