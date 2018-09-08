﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class ClassifiedSpanVisitor : SyntaxRewriter
    {
        private RazorSourceDocument _source;
        private List<ClassifiedSpanInternal> _spans;
        private BlockKindInternal _currentBlockKind;
        private SyntaxNode _currentBlock;

        public ClassifiedSpanVisitor(RazorSourceDocument source)
        {
            _source = source;
            _spans = new List<ClassifiedSpanInternal>();
            _currentBlockKind = BlockKindInternal.Markup;
        }

        public IReadOnlyList<ClassifiedSpanInternal> ClassifiedSpans => _spans;

        public override SyntaxNode VisitRazorCommentBlock(RazorCommentBlockSyntax node)
        {
            return WriteBlock(node, BlockKindInternal.Comment, base.VisitRazorCommentBlock);
        }

        public override SyntaxNode VisitCSharpCodeBlock(CSharpCodeBlockSyntax node)
        {
            if (node.Parent is CSharpStatementBodySyntax ||
                node.Parent is CSharpExpressionBodySyntax ||
                node.Parent is CSharpImplicitExpressionBodySyntax ||
                node.Parent is CSharpDirectiveBodySyntax)
            {
                return base.VisitCSharpCodeBlock(node);
            }

            return WriteBlock(node, BlockKindInternal.Statement, base.VisitCSharpCodeBlock);
        }

        public override SyntaxNode VisitCSharpStatement(CSharpStatement node)
        {
            return WriteBlock(node, BlockKindInternal.Statement, base.VisitCSharpStatement);
        }

        public override SyntaxNode VisitCSharpExpression(CSharpExpression node)
        {
            return WriteBlock(node, BlockKindInternal.Expression, base.VisitCSharpExpression);
        }

        public override SyntaxNode VisitCSharpImplicitExpression(CSharpImplicitExpression node)
        {
            return WriteBlock(node, BlockKindInternal.Expression, base.VisitCSharpImplicitExpression);
        }

        public override SyntaxNode VisitCSharpDirective(CSharpDirectiveSyntax node)
        {
            return WriteBlock(node, BlockKindInternal.Directive, base.VisitCSharpDirective);
        }

        public override SyntaxNode VisitCSharpTemplateBlock(CSharpTemplateBlockSyntax node)
        {
            return WriteBlock(node, BlockKindInternal.Template, base.VisitCSharpTemplateBlock);
        }

        public override SyntaxNode VisitHtmlMarkupBlock(HtmlMarkupBlockSyntax node)
        {
            return WriteBlock(node, BlockKindInternal.Markup, base.VisitHtmlMarkupBlock);
        }

        public override SyntaxNode VisitHtmlTagBlock(HtmlTagBlockSyntax node)
        {
            return WriteBlock(node, BlockKindInternal.Tag, base.VisitHtmlTagBlock);
        }

        public override SyntaxNode VisitHtmlAttributeBlock(HtmlAttributeBlockSyntax node)
        {
            return WriteBlock(node, BlockKindInternal.Markup, n =>
            {
                var equalsSyntax = SyntaxFactory.HtmlTextLiteral(new SyntaxList<SyntaxToken>(node.EqualsToken));
                var mergedAttributePrefix = MergeTextLiteralSpans(node.NamePrefix, node.Name, node.NameSuffix, equalsSyntax, node.ValuePrefix);
                Visit(mergedAttributePrefix);
                Visit(node.Value);
                Visit(node.ValueSuffix);

                return n;
            });
        }

        public override SyntaxNode VisitHtmlMinimizedAttributeBlock(HtmlMinimizedAttributeBlockSyntax node)
        {
            return WriteBlock(node, BlockKindInternal.Markup, n =>
            {
                var mergedAttributePrefix = MergeTextLiteralSpans(node.NamePrefix, node.Name);
                Visit(mergedAttributePrefix);

                return n;
            });
        }

        public override SyntaxNode VisitHtmlCommentBlock(HtmlCommentBlockSyntax node)
        {
            return WriteBlock(node, BlockKindInternal.HtmlComment, base.VisitHtmlCommentBlock);
        }

        public override SyntaxNode VisitHtmlDynamicAttributeValue(HtmlDynamicAttributeValueSyntax node)
        {
            return WriteBlock(node, BlockKindInternal.Markup, base.VisitHtmlDynamicAttributeValue);
        }

        public override SyntaxNode VisitRazorMetaCode(RazorMetaCodeSyntax node)
        {
            WriteSpan(node, SpanKindInternal.MetaCode);
            return base.VisitRazorMetaCode(node);
        }

        public override SyntaxNode VisitCSharpTransition(CSharpTransitionSyntax node)
        {
            WriteSpan(node, SpanKindInternal.Transition);
            return base.VisitCSharpTransition(node);
        }

        public override SyntaxNode VisitHtmlTransition(HtmlTransitionSyntax node)
        {
            WriteSpan(node, SpanKindInternal.Transition);
            return base.VisitHtmlTransition(node);
        }

        public override SyntaxNode VisitCSharpStatementLiteral(CSharpStatementLiteralSyntax node)
        {
            WriteSpan(node, SpanKindInternal.Code);
            return base.VisitCSharpStatementLiteral(node);
        }

        public override SyntaxNode VisitCSharpExpressionLiteral(CSharpExpressionLiteralSyntax node)
        {
            WriteSpan(node, SpanKindInternal.Code);
            return base.VisitCSharpExpressionLiteral(node);
        }

        public override SyntaxNode VisitCSharpDirectiveLiteral(CSharpDirectiveLiteralSyntax node)
        {
            WriteSpan(node, SpanKindInternal.Code);
            return base.VisitCSharpDirectiveLiteral(node);
        }

        public override SyntaxNode VisitCSharpCodeLiteral(CSharpCodeLiteralSyntax node)
        {
            WriteSpan(node, SpanKindInternal.Code);
            return base.VisitCSharpCodeLiteral(node);
        }

        public override SyntaxNode VisitCSharpLiteral(CSharpLiteralSyntax node)
        {
            WriteSpan(node, SpanKindInternal.None);
            return base.VisitCSharpLiteral(node);
        }

        public override SyntaxNode VisitHtmlLiteralAttributeValue(HtmlLiteralAttributeValueSyntax node)
        {
            WriteSpan(node, SpanKindInternal.Markup);
            return base.VisitHtmlLiteralAttributeValue(node);
        }

        public override SyntaxNode VisitHtmlTextLiteral(HtmlTextLiteralSyntax node)
        {
            if (node.Parent is HtmlLiteralAttributeValueSyntax)
            {
                return base.VisitHtmlTextLiteral(node);
            }

            WriteSpan(node, SpanKindInternal.Markup);
            return base.VisitHtmlTextLiteral(node);
        }

        private SyntaxNode WriteBlock<TNode>(TNode node, BlockKindInternal kind, Func<TNode, SyntaxNode> handler) where TNode : SyntaxNode
        {
            var previousBlock = _currentBlock;
            var previousKind = _currentBlockKind;

            _currentBlock = node;
            _currentBlockKind = kind;

            var result = handler(node);

            _currentBlock = previousBlock;
            _currentBlockKind = previousKind;

            return result;
        }

        private void WriteSpan(SyntaxNode node, SpanKindInternal kind)
        {
            if (node.IsMissing)
            {
                return;
            }

            var spanSource = GetSourceSpanForPosition(node.Position, node.FullWidth);
            var blockSource = GetSourceSpanForPosition(_currentBlock.Position, _currentBlock.FullWidth);
            var acceptedCharacters = AcceptedCharactersInternal.Any;
            var annotation = node.GetAnnotationValue(SyntaxConstants.SpanContextKind);
            if (annotation is SpanContext context)
            {
                acceptedCharacters = context.EditHandler.AcceptedCharacters;
            }

            var span = new ClassifiedSpanInternal(spanSource, blockSource, kind, _currentBlockKind, acceptedCharacters);
            _spans.Add(span);
        }

        private HtmlTextLiteralSyntax MergeTextLiteralSpans(params HtmlTextLiteralSyntax[] literalSyntaxes)
        {
            if (literalSyntaxes == null || literalSyntaxes.Length == 0)
            {
                return null;
            }

            SyntaxNode parent = null;
            var position = 0;
            var seenFirstLiteral = false;
            var builder = Syntax.InternalSyntax.SyntaxListBuilder.Create();

            foreach (var syntax in literalSyntaxes)
            {
                if (syntax == null)
                {
                    continue;
                }
                else if (!seenFirstLiteral)
                {
                    parent = syntax.Parent;
                    position = syntax.Position;
                    seenFirstLiteral = true;
                }

                foreach (var token in syntax.TextTokens)
                {
                    builder.Add(token.Green);
                }
            }

            var mergedLiteralSyntax = Syntax.InternalSyntax.SyntaxFactory.HtmlTextLiteral(
                builder.ToList<Syntax.InternalSyntax.SyntaxToken>());

            return (HtmlTextLiteralSyntax)mergedLiteralSyntax.CreateRed(parent, position);
        }

        private SourceSpan GetSourceSpanForPosition(int absoluteIndex, int length)
        {
            try
            {
                var location = _source.Lines.GetLocation(absoluteIndex);
                return new SourceSpan(location, length);
            }
            catch (IndexOutOfRangeException)
            {
                return new SourceSpan(_source.FilePath, absoluteIndex, 0, 0, length);
            }
        }
    }
}
