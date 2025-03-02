﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using static Roslynator.Logger;

namespace Roslynator.Formatting
{
    internal static class CodeFormatter
    {
        public static Task<Project> FormatProjectAsync(
            Project project,
            ISyntaxFactsService syntaxFacts,
            CancellationToken cancellationToken = default)
        {
            return FormatProjectAsync(project, syntaxFacts, default(CodeFormatterOptions), cancellationToken);
        }

        public static async Task<Project> FormatProjectAsync(
            Project project,
            ISyntaxFactsService syntaxFacts,
            CodeFormatterOptions options,
            CancellationToken cancellationToken = default)
        {
            if (options == null)
                options = CodeFormatterOptions.Default;

            foreach (DocumentId documentId in project.DocumentIds)
            {
                Document document = project.GetDocument(documentId);

                if (options.IncludeGeneratedCode
                    || !GeneratedCodeUtility.IsGeneratedCodeFile(document.FilePath))
                {
                    SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                    if (options.IncludeGeneratedCode
                        || !syntaxFacts.BeginsWithAutoGeneratedComment(root))
                    {
                        DocumentOptionSet optionSet = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

                        Document newDocument = await Formatter.FormatAsync(document, optionSet, cancellationToken).ConfigureAwait(false);

                        project = newDocument.Project;
                    }
                }
            }

            return project;
        }

        internal static async Task<ImmutableArray<DocumentId>> GetFormattedDocumentsAsync(
            Project project,
            Project newProject,
            ISyntaxFactsService syntaxFacts)
        {
            ImmutableArray<DocumentId>.Builder builder = null;

            //TODO: GetChangedDocuments(onlyGetDocumentsWithTextChanges: true)
            foreach (DocumentId documentId in newProject
                .GetChanges(project)
                .GetChangedDocuments())
            {
                Document document = newProject.GetDocument(documentId);

                // https://github.com/dotnet/roslyn/issues/30674
                if ((await document.GetTextChangesAsync(project.GetDocument(documentId)).ConfigureAwait(false)).Any())
                {
#if DEBUG
                    bool success = await VerifySyntaxEquivalenceAsync(project.GetDocument(document.Id), document, syntaxFacts).ConfigureAwait(false);
#endif
                    (builder ??= ImmutableArray.CreateBuilder<DocumentId>()).Add(document.Id);
                }
            }

            return builder?.ToImmutableArray() ?? ImmutableArray<DocumentId>.Empty;
        }

#if DEBUG
        private static async Task<bool> VerifySyntaxEquivalenceAsync(
            Document oldDocument,
            Document newDocument,
            ISyntaxFactsService syntaxFacts,
            CancellationToken cancellationToken = default)
        {
            if (!string.Equals(
                (await newDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false)).NormalizeWhitespace("", false).ToFullString(),
                (await oldDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false)).NormalizeWhitespace("", false).ToFullString(),
                StringComparison.Ordinal))
            {
                WriteLine($"Syntax roots with normalized white-space are not equivalent '{oldDocument.FilePath}'", ConsoleColors.Magenta);
                return false;
            }

            if (!syntaxFacts.AreEquivalent(
                await newDocument.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false),
                await oldDocument.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false)))
            {
                WriteLine($"Syntax trees are not equivalent '{oldDocument.FilePath}'", ConsoleColors.Magenta);
                return false;
            }

            return true;
        }
#endif
    }
}
