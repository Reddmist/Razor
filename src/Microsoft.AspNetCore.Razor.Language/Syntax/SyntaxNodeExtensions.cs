﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language.Syntax
{
    internal static class SyntaxNodeExtensions
    {
        public static TNode WithAnnotations<TNode>(this TNode node, params SyntaxAnnotation[] annotations) where TNode : SyntaxNode
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            return (TNode)node.Green.SetAnnotations(annotations).CreateRed(node.Parent, node.Position);
        }

        public static object GetAnnotationValue<TNode>(this TNode node, string key) where TNode : SyntaxNode
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var annotation = node.GetAnnotations().FirstOrDefault(n => n.Kind == key);
            return annotation?.Data;
        }

        public static TNode WithDiagnostics<TNode>(this TNode node, params RazorDiagnostic[] diagnostics) where TNode : SyntaxNode
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            return (TNode)node.Green.SetDiagnostics(diagnostics).CreateRed(node.Parent, node.Position);
        }

        public static TNode AppendDiagnostic<TNode>(this TNode node, params RazorDiagnostic[] diagnostics) where TNode : SyntaxNode
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var existingDiagnostics = node.GetDiagnostics();
            var allDiagnostics = existingDiagnostics.Concat(diagnostics).ToArray();

            return (TNode)node.WithDiagnostics(allDiagnostics);
        }

        public static SourceLocation GetSourceLocation(this SyntaxNode node, RazorSourceDocument source)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.Lines.GetLocation(node.Position);
        }

        public static SourceSpan GetSourceSpan(this SyntaxNode node, RazorSourceDocument source)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var location = node.GetSourceLocation(source);

            return new SourceSpan(location, node.FullWidth);
        }

        public static SpanContext GetSpanContext(this SyntaxNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var context = node.GetAnnotationValue(SyntaxConstants.SpanContextKind);

            return context is SpanContext ? (SpanContext)context : null;
        }

        /// <summary>
        /// Creates a new tree of nodes with the specified nodes, tokens and trivia replaced.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root node of the tree of nodes.</param>
        /// <param name="nodes">The nodes to be replaced.</param>
        /// <param name="computeReplacementNode">A function that computes a replacement node for the
        /// argument nodes. The first argument is the original node. The second argument is the same
        /// node potentially rewritten with replaced descendants.</param>
        public static TRoot ReplaceSyntax<TRoot>(
            this TRoot root,
            IEnumerable<SyntaxNode> nodes,
            Func<SyntaxNode, SyntaxNode, SyntaxNode> computeReplacementNode)
            where TRoot : SyntaxNode
        {
            return (TRoot)root.ReplaceCore(
                nodes: nodes, computeReplacementNode: computeReplacementNode);
        }

        /// <summary>
        /// Creates a new tree of nodes with the specified old node replaced with a new node.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <typeparam name="TNode">The type of the nodes being replaced.</typeparam>
        /// <param name="root">The root node of the tree of nodes.</param>
        /// <param name="nodes">The nodes to be replaced; descendants of the root node.</param>
        /// <param name="computeReplacementNode">A function that computes a replacement node for the
        /// argument nodes. The first argument is the original node. The second argument is the same
        /// node potentially rewritten with replaced descendants.</param>
        public static TRoot ReplaceNodes<TRoot, TNode>(this TRoot root, IEnumerable<TNode> nodes, Func<TNode, TNode, SyntaxNode> computeReplacementNode)
            where TRoot : SyntaxNode
            where TNode : SyntaxNode
        {
            return (TRoot)root.ReplaceCore(nodes: nodes, computeReplacementNode: computeReplacementNode);
        }

        /// <summary>
        /// Creates a new tree of nodes with the specified old node replaced with a new node.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root node of the tree of nodes.</param>
        /// <param name="oldNode">The node to be replaced; a descendant of the root node.</param>
        /// <param name="newNode">The new node to use in the new tree in place of the old node.</param>
        public static TRoot ReplaceNode<TRoot>(this TRoot root, SyntaxNode oldNode, SyntaxNode newNode)
            where TRoot : SyntaxNode
        {
            if (oldNode == newNode)
            {
                return root;
            }

            return (TRoot)root.ReplaceCore(nodes: new[] { oldNode }, computeReplacementNode: (o, r) => newNode);
        }

        /// <summary>
        /// Creates a new tree of nodes with specified old node replaced with a new nodes.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root of the tree of nodes.</param>
        /// <param name="oldNode">The node to be replaced; a descendant of the root node and an element of a list member.</param>
        /// <param name="newNodes">A sequence of nodes to use in the tree in place of the old node.</param>
        public static TRoot ReplaceNode<TRoot>(this TRoot root, SyntaxNode oldNode, IEnumerable<SyntaxNode> newNodes)
            where TRoot : SyntaxNode
        {
            return (TRoot)root.ReplaceNodeInListCore(oldNode, newNodes);
        }

        /// <summary>
        /// Creates a new tree of nodes with new nodes inserted before the specified node.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root of the tree of nodes.</param>
        /// <param name="nodeInList">The node to insert before; a descendant of the root node an element of a list member.</param>
        /// <param name="newNodes">A sequence of nodes to insert into the tree immediately before the specified node.</param>
        public static TRoot InsertNodesBefore<TRoot>(this TRoot root, SyntaxNode nodeInList, IEnumerable<SyntaxNode> newNodes)
            where TRoot : SyntaxNode
        {
            return (TRoot)root.InsertNodesInListCore(nodeInList, newNodes, insertBefore: true);
        }

        /// <summary>
        /// Creates a new tree of nodes with new nodes inserted after the specified node.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root of the tree of nodes.</param>
        /// <param name="nodeInList">The node to insert after; a descendant of the root node an element of a list member.</param>
        /// <param name="newNodes">A sequence of nodes to insert into the tree immediately after the specified node.</param>
        public static TRoot InsertNodesAfter<TRoot>(this TRoot root, SyntaxNode nodeInList, IEnumerable<SyntaxNode> newNodes)
            where TRoot : SyntaxNode
        {
            return (TRoot)root.InsertNodesInListCore(nodeInList, newNodes, insertBefore: false);
        }
    }
}