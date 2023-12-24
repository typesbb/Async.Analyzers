using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TaskAsyncInsteadCodeFixProvider)), Shared]
public class TaskAsyncInsteadCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(TaskAsyncInsteadAnalyzer.DiagnosticId);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics.First();
        var invocationExpr = root.FindNode(diagnostic.Location.SourceSpan) as InvocationExpressionSyntax;
        if (invocationExpr != null)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Replace with async method",
                    createChangedDocument: c => ReplaceWithAsyncMethod(context.Document, invocationExpr, c),
                    equivalenceKey: "Replace with async method"),
                diagnostic);
        }
    }

    private async Task<Document> ReplaceWithAsyncMethod(Document document, InvocationExpressionSyntax invocationExpr, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken);
        IMethodSymbol methodSymbol = semanticModel.GetSymbolInfo(invocationExpr).Symbol as IMethodSymbol;

        // Find async methods with same name as current method
        var asyncMethod = methodSymbol.ContainingType.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.ReturnType is INamedTypeSymbol typeSymbol && typeSymbol != null && typeSymbol.IsTaskType(semanticModel) && (m.Name == methodSymbol.Name || m.Name == methodSymbol.Name + "Async"))
            .Where(m => Enumerable.SequenceEqual(m.Parameters, methodSymbol.Parameters, SymbolEqualityComparer.Default))
            .FirstOrDefault();
        if (methodSymbol != null)
        {
            // Replace method invocation with the async method
            ExpressionSyntax newExpression = null;
            if (invocationExpr.Expression is MemberAccessExpressionSyntax memberAccessExpr && memberAccessExpr != null)
            {
                newExpression = SyntaxFactory.MemberAccessExpression(memberAccessExpr.Kind(), memberAccessExpr.Expression, SyntaxFactory.IdentifierName(asyncMethod.Name));
            }
            else if (invocationExpr.Expression is IdentifierNameSyntax identifierName && identifierName != null)
            {
                newExpression = SyntaxFactory.IdentifierName(asyncMethod.Name);
            }
            var newInvocation = SyntaxFactory.InvocationExpression(newExpression, invocationExpr.ArgumentList);
            ExpressionSyntax awaitExpression = SyntaxFactory.AwaitExpression(newInvocation).WithLeadingTrivia(invocationExpr.GetLeadingTrivia()).WithTrailingTrivia(invocationExpr.GetTrailingTrivia());

            if (invocationExpr.Parent is ExpressionSyntax expressionSyntax && expressionSyntax != null && expressionSyntax.IsPrecedenceGreaterThanAwait())
            {
                awaitExpression = SyntaxFactory.ParenthesizedExpression(awaitExpression)
                    .WithAdditionalAnnotations(Formatter.Annotation);
            }

            editor.ReplaceNode(invocationExpr, awaitExpression);
        }

        return editor.GetChangedDocument();
    }
}
