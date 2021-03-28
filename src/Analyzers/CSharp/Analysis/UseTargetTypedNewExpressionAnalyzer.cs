// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslynator.CSharp.Analysis
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseTargetTypedNewExpressionAnalyzer : BaseDiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(DiagnosticDescriptors.UseTargetTypedNewExpression); }
        }

        public override void Initialize(AnalysisContext context)
        {
            base.Initialize(context);

            context.RegisterSyntaxNodeAction(f => AnalyzeObjectCreationExpression(f), SyntaxKind.ObjectCreationExpression);
        }

        private static void AnalyzeObjectCreationExpression(SyntaxNodeAnalysisContext context)
        {
            var objectCreation = (ObjectCreationExpressionSyntax)context.Node;

            SyntaxNode parent = objectCreation.Parent;

            InitializerExpressionSyntax initializer = objectCreation.Initializer;

            if (initializer != null)
            {
                foreach (ExpressionSyntax expression in initializer.Expressions)
                {
                    if (expression.IsKind(SyntaxKind.ObjectCreationExpression))
                        return;
                }
            }

            switch (parent.Kind())
            {
                case SyntaxKind.ThrowExpression:
                case SyntaxKind.ThrowStatement:
                    {
                        if (context.SemanticModel.GetTypeSymbol(objectCreation, context.CancellationToken)?
                            .HasMetadataName(MetadataNames.System_Exception) == true)
                        {
                            ReportDiagnostic(context, objectCreation);
                        }

                        break;
                    }
                case SyntaxKind.EqualsValueClause:
                    {
                        parent = parent.Parent;

                        Debug.Assert(parent.IsKind(SyntaxKind.VariableDeclarator, SyntaxKind.PropertyDeclaration), parent.ToDebugString());

                        if (parent.IsKind(SyntaxKind.VariableDeclarator))
                        {
                            parent = parent.Parent;

                            if (parent.IsKind(SyntaxKind.VariableDeclaration))
                            {
                                AnalyzeType(context, objectCreation, ((VariableDeclarationSyntax)parent).Type);
                                return;
                            }
                        }
                        else if (parent.IsKind(SyntaxKind.PropertyDeclaration))
                        {
                            AnalyzeType(context, objectCreation, ((PropertyDeclarationSyntax)parent).Type);
                            return;
                        }

                        break;
                    }
                case SyntaxKind.ReturnStatement:
                case SyntaxKind.YieldReturnStatement:
                    {
                        for (SyntaxNode node = parent.Parent; node != null; node = node.Parent)
                        {
                            if (CSharpFacts.IsAnonymousFunctionExpression(node.Kind()))
                                return;

                            TypeSyntax type = DetermineReturnType(node);

                            if (type != null)
                            {
                                if (parent.IsKind(SyntaxKind.YieldReturnStatement))
                                {
                                    ITypeSymbol typeSymbol = context.SemanticModel.GetTypeSymbol(type, context.CancellationToken);

                                    if (typeSymbol?.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
                                    {
                                        var ienumerableOfT = (INamedTypeSymbol)typeSymbol;

                                        ITypeSymbol typeSymbol2 = ienumerableOfT.TypeArguments.Single();

                                        AnalyzeTypeSymbol(context, objectCreation, typeSymbol2);
                                    }

                                    return;
                                }

                                AnalyzeType(context, objectCreation, type);
                                return;
                            }
                        }

                        Debug.Fail(parent.ToDebugString());
                        break;
                    }
                case SyntaxKind.ArrowExpressionClause:
                    {
                        TypeSyntax type = DetermineReturnType(parent.Parent);

                        Debug.Assert(type != null, parent.ToDebugString());

                        if (type != null)
                            AnalyzeType(context, objectCreation, type);

                        break;
                    }
                case SyntaxKind.ArrayInitializerExpression:
                    {
                        if (parent.IsParentKind(SyntaxKind.ArrayCreationExpression))
                        {
                            var arrayCreationExpression = (ArrayCreationExpressionSyntax)parent.Parent;

                            AnalyzeType(context, objectCreation, arrayCreationExpression.Type.ElementType);
                            return;
                        }

                        Debug.Assert(parent.IsParentKind(SyntaxKind.ImplicitArrayCreationExpression), parent.Parent.ToDebugString());
                        break;
                    }
                case SyntaxKind.CollectionInitializerExpression:
                    {
                        Debug.Assert(parent.IsParentKind(SyntaxKind.ObjectCreationExpression, SyntaxKind.SimpleAssignmentExpression), parent.Parent.ToDebugString());
                        break;
                    }
                case SyntaxKind.SimpleAssignmentExpression:
                case SyntaxKind.CoalesceAssignmentExpression:
                case SyntaxKind.AddAssignmentExpression:
                case SyntaxKind.SubtractAssignmentExpression:
                    {
                        var assignment = (AssignmentExpressionSyntax)parent;

                        AnalyzeExpression(context, objectCreation, assignment.Left);
                        break;
                    }
                case SyntaxKind.CoalesceExpression:
                    {
                        var coalesceExpression = (BinaryExpressionSyntax)parent;

                        AnalyzeExpression(context, objectCreation, coalesceExpression.Left);
                        break;
                    }
                case SyntaxKind.AnonymousObjectMemberDeclarator:
                case SyntaxKind.Argument:
                case SyntaxKind.AsExpression:
                case SyntaxKind.CastExpression:
                case SyntaxKind.ComplexElementInitializerExpression:
                case SyntaxKind.ConditionalExpression:
                case SyntaxKind.ForEachStatement:
                case SyntaxKind.ForEachVariableStatement:
                case SyntaxKind.Interpolation:
                case SyntaxKind.ParenthesizedLambdaExpression:
                case SyntaxKind.SimpleLambdaExpression:
                case SyntaxKind.SimpleMemberAccessExpression:
                case SyntaxKind.ExpressionStatement:
                    {
                        break;
                    }
                default:
                    {
                        Debug.Fail(parent.ToDebugString());
                        break;
                    }
            }
        }

        private static void AnalyzeType(
            SyntaxNodeAnalysisContext context,
            ObjectCreationExpressionSyntax objectCreation,
            TypeSyntax type)
        {
            if (!type.IsVar)
                AnalyzeExpression(context, objectCreation, type);
        }

        private static void AnalyzeExpression(
            SyntaxNodeAnalysisContext context,
            ObjectCreationExpressionSyntax objectCreation,
            ExpressionSyntax expression)
        {
            ITypeSymbol typeSymbol1 = context.SemanticModel.GetTypeSymbol(expression);

            AnalyzeTypeSymbol(context, objectCreation, typeSymbol1);
        }

        private static void AnalyzeTypeSymbol(
            SyntaxNodeAnalysisContext context,
            ObjectCreationExpressionSyntax objectCreation,
            ITypeSymbol typeSymbol1)
        {
            if (typeSymbol1?.IsErrorType() == false)
            {
                ITypeSymbol typeSymbol2 = context.SemanticModel.GetTypeSymbol(objectCreation);

                if (SymbolEqualityComparer.Default.Equals(typeSymbol1, typeSymbol2))
                    ReportDiagnostic(context, objectCreation);
            }
        }

        private static TypeSyntax DetermineReturnType(SyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.LocalFunctionStatement:
                    {
                        return ((LocalFunctionStatementSyntax)node).ReturnType;
                    }
                case SyntaxKind.MethodDeclaration:
                    {
                        return ((MethodDeclarationSyntax)node).ReturnType;
                    }
                case SyntaxKind.OperatorDeclaration:
                    {
                        return ((OperatorDeclarationSyntax)node).ReturnType;
                    }
                case SyntaxKind.ConversionOperatorDeclaration:
                    {
                        return ((ConversionOperatorDeclarationSyntax)node).Type;
                    }
                case SyntaxKind.PropertyDeclaration:
                    {
                        return ((PropertyDeclarationSyntax)node).Type;
                    }
                case SyntaxKind.IndexerDeclaration:
                    {
                        return ((IndexerDeclarationSyntax)node).Type;
                    }
                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                case SyntaxKind.UnknownAccessorDeclaration:
                case SyntaxKind.InitAccessorDeclaration:
                    {
                        Debug.Assert(node.IsParentKind(SyntaxKind.AccessorList), node.Parent.ToDebugString());

                        if (node.IsParentKind(SyntaxKind.AccessorList))
                            return DetermineReturnType(node.Parent.Parent);

                        return null;
                    }
            }

            return null;
        }

        private static void ReportDiagnostic(SyntaxNodeAnalysisContext context, ObjectCreationExpressionSyntax objectCreation)
        {
            DiagnosticHelpers.ReportDiagnostic(context, DiagnosticDescriptors.UseTargetTypedNewExpression, objectCreation.Type);
        }
    }
}
