// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslynator.CodeFixes;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Roslynator.CSharp.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseTargetTypedNewExpressionCodeFixProvider))]
    [Shared]
    public class UseTargetTypedNewExpressionCodeFixProvider : BaseCodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(DiagnosticIdentifiers.UseTargetTypedNewExpression); }
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode root = await context.GetSyntaxRootAsync().ConfigureAwait(false);

            if (!TryFindFirstAncestorOrSelf(root, context.Span, out ObjectCreationExpressionSyntax objectCreation))
                return;

            Document document = context.Document;
            Diagnostic diagnostic = context.Diagnostics[0];

            CodeAction codeAction = CodeAction.Create(
                "Use implicit type",
                ct =>
                {
                    ImplicitObjectCreationExpressionSyntax implicitObjectCreation = ImplicitObjectCreationExpression(
                        objectCreation.NewKeyword.WithTrailingTrivia(objectCreation.NewKeyword.TrailingTrivia.EmptyIfWhitespace()),
                        objectCreation.ArgumentList ?? ArgumentList().WithTrailingTrivia(objectCreation.Type.GetTrailingTrivia()),
                        objectCreation.Initializer);

                    return document.ReplaceNodeAsync(objectCreation, implicitObjectCreation, ct);
                },
                GetEquivalenceKey(diagnostic));

            context.RegisterCodeFix(codeAction, diagnostic);
        }
    }
}
