using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Text;

public static class TaskExtensions
{
    public static bool IsTaskType(this INamedTypeSymbol typeSymbol, SemanticModel semanticModel)
    {
        if (typeSymbol.Equals(semanticModel.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task")) ||
            typeSymbol.ConstructedFrom.Equals(semanticModel.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1")) ||
            typeSymbol.Equals(semanticModel.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask")) ||
            typeSymbol.ConstructedFrom.Equals(semanticModel.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1")) ||
            typeSymbol.Equals(semanticModel.Compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.ConfiguredTaskAwaitable")) ||
            typeSymbol.ConstructedFrom.Equals(semanticModel.Compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.ConfiguredTaskAwaitable`1")) ||
            typeSymbol.Equals(semanticModel.Compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable")) ||
            typeSymbol.ConstructedFrom.Equals(semanticModel.Compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable`1")))
            return true;

        return false;
    }
    public static bool IsPrecedenceGreaterThanAwait(this ExpressionSyntax expression)
    {
        if (expression is MemberAccessExpressionSyntax ||
            expression is InvocationExpressionSyntax ||
            expression is ElementAccessExpressionSyntax ||
            expression is ConditionalAccessExpressionSyntax ||
            expression is PostfixUnaryExpressionSyntax ||
            expression is ObjectCreationExpressionSyntax ||
            expression is ArrayCreationExpressionSyntax ||
            expression is ImplicitArrayCreationExpressionSyntax ||
            expression is TypeOfExpressionSyntax ||
            expression is CheckedExpressionSyntax ||
            expression is DefaultExpressionSyntax ||
            expression is AnonymousMethodExpressionSyntax ||
            expression is SizeOfExpressionSyntax ||
            expression is StackAllocArrayCreationExpressionSyntax ||
            expression is PointerTypeSyntax)
        {
            return true;
        }
        return false;
    }
}
