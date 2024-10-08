using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Async.Analyzers
{
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
            if (!(context.SemanticModel.GetSymbolInfo(invocationExpr).Symbol is IMethodSymbol methodSymbol))
            {
                return;
            }
            if (methodSymbol.ReturnType is INamedTypeSymbol typeSymbol1 && typeSymbol1 != null && typeSymbol1.IsTaskType(context.SemanticModel))
            {
                // Skip if the method is already async
                return;
            }

            if (methodSymbol == null)
                return;
            if (methodSymbol.ContainingType == null)
                return;
            if (methodSymbol.Name.EndsWith("Async"))
                return;

            // Find async methods with same name as current method
            var asyncMethod = methodSymbol.ContainingType.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => m.ReturnType is INamedTypeSymbol typeSymbol
                    && typeSymbol != null
                    && typeSymbol.IsTaskType(context.SemanticModel)
                    && m.Name == methodSymbol.Name + "Async")
                .Where(m => Enumerable.SequenceEqual(m.Parameters, methodSymbol.Parameters, SymbolEqualityComparer.Default))
                .FirstOrDefault();
            if (asyncMethod != null)
            {
                var diagnostic = Diagnostic.Create(Rule, invocationExpr.GetLocation(), methodSymbol.Name, asyncMethod.Name);
                context.ReportDiagnostic(diagnostic);
            }
            else if (invocationExpr.Expression is MemberAccessExpressionSyntax memberAccessExpr)
            {
                var typeInfo = context.SemanticModel.GetTypeInfo(memberAccessExpr.Expression);
                // Check all classes in the current solution for the async method
                var allTypes = GetAllTypes(context.Compilation.GlobalNamespace);
                asyncMethod = allTypes
                    .SelectMany(typeSymbol => typeSymbol.GetMembers().OfType<IMethodSymbol>())
                    .Where(m => m.IsExtensionMethod
                        && m.ReturnType is INamedTypeSymbol typeSymbol
                        && typeSymbol != null
                        && typeSymbol.IsTaskType(context.SemanticModel)
                        && m.Name == methodSymbol.Name + "Async")
                    .Where(m => IsSubtypeOf(typeInfo.Type.OriginalDefinition, m.Parameters.First().Type.OriginalDefinition))
                    .Where(m => Enumerable.SequenceEqual(
                        m.Parameters.Skip(1).Where(e => !e.IsThis && !e.IsOptional).Select(e => e.Type.OriginalDefinition),
                        methodSymbol.Parameters.Where(e => !e.IsThis && !e.IsOptional).Select(e => e.Type.OriginalDefinition),
                        SymbolEqualityComparer.Default))
                    .FirstOrDefault();

                if (asyncMethod != null)
                {
                    if (IsInIQueryableLinqQuery(invocationExpr, context.SemanticModel))
                    {
                        return;
                    }
                    var diagnostic = Diagnostic.Create(Rule, invocationExpr.GetLocation(), methodSymbol.Name, asyncMethod.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
        private static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol namespaceSymbol)
        {
            foreach (var member in namespaceSymbol.GetMembers())
            {
                if (member is INamedTypeSymbol namedType && namedType != null && namedType.IsStatic)
                {
                    yield return namedType;
                }
                else if (member is INamespaceSymbol childNamespace)
                {
                    foreach (var nestedType in GetAllTypes(childNamespace))
                    {
                        if (!nestedType.IsStatic)
                            continue;
                        yield return nestedType;
                    }
                }
            }
        }
        private static bool IsSubtypeOf(ITypeSymbol type, ITypeSymbol baseType)
        {
            if (type == null || baseType == null)
            {
                return false;
            }

            // 检查类型自身是否与基类类型一致
            if (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, baseType.OriginalDefinition))
            {
                return true;
            }

            // 检查类型的基类
            var currentBaseType = type.BaseType;
            while (currentBaseType != null)
            {
                if (SymbolEqualityComparer.Default.Equals(currentBaseType.OriginalDefinition, baseType.OriginalDefinition))
                {
                    return true;
                }
                currentBaseType = currentBaseType.BaseType;
            }

            // 检查实现的接口
            foreach (var interfaceType in type.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(interfaceType.OriginalDefinition, baseType.OriginalDefinition))
                {
                    return true;
                }
            }

            // 若无继承关系
            return false;
        }
        private static bool IsInIQueryableLinqQuery(SyntaxNode node, SemanticModel semanticModel)
        {
            if (node == null || semanticModel == null)
            {
                return false;
            }

            // 当前节点的父节点
            SyntaxNode current = node.Parent;

            while (current != null)
            {
                if (current is QueryExpressionSyntax || current is InvocationExpressionSyntax)
                {
                    // 获取当前表达式的类型信息
                    var typeInfo = semanticModel.GetTypeInfo(current);
                    if (typeInfo.Type != null && IsIQueryableType(typeInfo.Type))
                    {
                        return true;
                    }
                }

                // 移动到上一个父节点
                current = current.Parent;
            }

            return false;
            bool IsIQueryableType(ITypeSymbol typeSymbol)
            {
                var iqueryableType = typeSymbol
                    .AllInterfaces
                    .FirstOrDefault(i => i.OriginalDefinition.ToString() == "System.Linq.IQueryable");

                return iqueryableType != null;
            }
        }
    }
}