using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TaskResultCodeFixProvider)), Shared]
public class TaskResultCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(TaskResultAnalyzer.DiagnosticId);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;
        var memberAccessExpr = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf()
            .OfType<MemberAccessExpressionSyntax>()
            .Where(e => e is MemberAccessExpressionSyntax accessor && accessor != null && accessor.Name.Identifier.Text == "Result")
            .First();

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Use 'await'",
                createChangedDocument: c => ReplaceWithAwaitAsync(context.Document, memberAccessExpr, c),
                equivalenceKey: "Use 'await'"),
            diagnostic);
    }

    private async Task<Document> ReplaceWithAwaitAsync(Document document, MemberAccessExpressionSyntax memberAccessExpr, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        var newExpression = memberAccessExpr.Expression;

        ExpressionSyntax awaitExpression = SyntaxFactory.AwaitExpression(newExpression).WithLeadingTrivia(memberAccessExpr.GetLeadingTrivia()).WithTrailingTrivia(memberAccessExpr.GetTrailingTrivia());

        if (memberAccessExpr.Parent is ExpressionSyntax expressionSyntax && expressionSyntax != null && expressionSyntax.IsPrecedenceGreaterThanAwait())
        {
            awaitExpression = SyntaxFactory.ParenthesizedExpression(awaitExpression)
                .WithAdditionalAnnotations(Formatter.Annotation);
        }

        editor.ReplaceNode(memberAccessExpr, awaitExpression);
        return editor.GetChangedDocument();
    }
}
