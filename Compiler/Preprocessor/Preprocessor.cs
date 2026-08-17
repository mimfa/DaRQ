// Converted from src/engine/DaRQ/Compiler/Preprocessor/index.ts
using System;
using System.Collections.Generic;
using System.Linq;
using MiMFa.DaRQ.Compiler.Core;

namespace MiMFa.DaRQ.Compiler.Preprocessor
{
    public abstract class Preprocessor : IStage
    {
        public MiMFa.DaRQ.Compiler.Compiler? Compiler { get; set; }

        public object Transform(object input, MiMFa.DaRQ.Compiler.Compiler? compiler)
        {
            Token[] tokens = input as Token[] ?? (input as IEnumerable<Token>)?.ToArray() ?? new Token[0];
            var walker = new Parser.TokenWalker(tokens, compiler?.Input?.Source);
            return Preprocess(walker, compiler).ToArray();
        }

        public IEnumerable<Token> Preprocess(Parser.TokenWalker walker, MiMFa.DaRQ.Compiler.Compiler? compiler = null)
        {
            if (compiler != null) this.Compiler = compiler;
            while (!walker.IsEnded)
                yield return PreprocessToken(walker.Walk(), walker);
        }

        public virtual Token PreprocessToken(Token token, Parser.TokenWalker walker) => token;
    }
}
