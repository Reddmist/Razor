﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class SpanContext
    {
        public SpanContext(ISpanChunkGenerator chunkGenerator, SpanEditHandler editHandler)
        {
            ChunkGenerator = chunkGenerator;
            EditHandler = editHandler;
        }

        public ISpanChunkGenerator ChunkGenerator { get; }

        public SpanEditHandler EditHandler { get; }
    }

    internal class SpanContextBuilder
    {
        public SpanContextBuilder()
        {
            LastAcceptedCharacters = AcceptedCharactersInternal.Any;
            Reset();
        }

        public ISpanChunkGenerator ChunkGenerator { get; set; }

        public SpanEditHandler EditHandler { get; set; }

        public AcceptedCharactersInternal LastAcceptedCharacters { get; private set; }

        public SpanContext Build()
        {
            var result = new SpanContext(ChunkGenerator, EditHandler);
            LastAcceptedCharacters = EditHandler.AcceptedCharacters;
            Reset();
            return result;
        }

        public void Reset()
        {
            EditHandler = SpanEditHandler.CreateDefault((content) => Enumerable.Empty<SyntaxToken>());
            ChunkGenerator = SpanChunkGenerator.Null;
        }
    }
}
