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

namespace Async.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TaskAsyncInsteadCodeFixProvider)), Shared]
    public class TaskAsyncInsteadCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(TaskAsyncInsteadAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var syntaxNode = root.FindNode(diagnostic.Location.SourceSpan);
            var invocationExpr = syntaxNode as InvocationExpressionSyntax;
            if (invocationExpr == null)
            {
                //ArgumentSyntax
                invocationExpr = syntaxNode.ChildNodes().First() as InvocationExpressionSyntax;
            }
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

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            if (semanticModel == null)
                return editor.GetChangedDocument();
            var methodSymbol = semanticModel.GetSymbolInfo(invocationExpr).Symbol as IMethodSymbol;
            if (methodSymbol == null)
                return editor.GetChangedDocument();

            // Replace method invocation with the async method
            ExpressionSyntax newExpression = null;
            if (invocationExpr.Expression is MemberAccessExpressionSyntax memberAccessExpr && memberAccessExpr != null)
            {
                SimpleNameSyntax simpleNameSyntax;
                if (memberAccessExpr.Name is GenericNameSyntax genericNameSyntax && genericNameSyntax != null)
                {
                    simpleNameSyntax = SyntaxFactory.GenericName(SyntaxFactory.Identifier(methodSymbol.Name + "Async"), genericNameSyntax.TypeArgumentList);
                }
                else
                {
                    simpleNameSyntax = SyntaxFactory.IdentifierName(methodSymbol.Name + "Async");
                }
                newExpression = SyntaxFactory.MemberAccessExpression(memberAccessExpr.Kind(), memberAccessExpr.Expression, simpleNameSyntax);
            }
            else if (invocationExpr.Expression is IdentifierNameSyntax identifierName && identifierName != null)
            {
                newExpression = SyntaxFactory.IdentifierName(methodSymbol.Name + "Async");
            }
            else if (invocationExpr.Expression is GenericNameSyntax genericNameSyntax && genericNameSyntax != null)
            {
                newExpression = SyntaxFactory.GenericName(SyntaxFactory.Identifier(methodSymbol.Name + "Async"), genericNameSyntax.TypeArgumentList);
            }
            else
            {
                return editor.GetChangedDocument();
            }
            var newInvocation = SyntaxFactory.InvocationExpression(newExpression, invocationExpr.ArgumentList);
            ExpressionSyntax awaitExpression = SyntaxFactory.AwaitExpression(newInvocation).WithLeadingTrivia(invocationExpr.GetLeadingTrivia()).WithTrailingTrivia(invocationExpr.GetTrailingTrivia());

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