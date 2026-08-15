// Converted from src/engine/DaRQ/Compiler/Preprocessor/index.ts
using System;
using System.Collections.Generic;
using DaRQ.Compiler.Core;

namespace DaRQ.Compiler.Preprocessor
{
    public abstract class Preprocessor : IStage
    {
        public DaRQ.Compiler.Compiler Compiler { get; set; }

        public object Transform(object input, DaRQ.Compiler.Compiler compiler)
        {
            var tokens = (Token[])input;
            var walker = new Parser.TokenWalker(tokens, compiler?.Input?.Source);
            return Preprocess(walker, compiler).ToArray();
        }

        public IEnumerable<Token> Preprocess(Parser.TokenWalker walker, DaRQ.Compiler.Compiler compiler = null)
        {
            if (compiler != null) this.Compiler = compiler;
            while (!walker.IsEnded)
                yield return PreprocessToken(walker.Walk(), walker);
        }

        public virtual Token PreprocessToken(Token token, Parser.TokenWalker walker) => token;
    }
}
