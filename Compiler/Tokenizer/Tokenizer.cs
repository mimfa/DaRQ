// Converted from src/engine/DaRQ/Compiler/Tokenizer/index.ts
using System;
using System.Collections.Generic;
using System.Linq;
using MiMFa.DaRQ.Compiler.Core;

namespace MiMFa.DaRQ.Compiler.Tokenizer
{
    public abstract class Tokenizer : IStage
    {
        public MiMFa.DaRQ.Compiler.Compiler? Compiler { get; set; }

        public object Transform(object input, MiMFa.DaRQ.Compiler.Compiler? compiler)
        {
            var walker = new CodeWalker((string)input, compiler?.Input?.Source);
            return Tokenize(walker, compiler).ToArray();
        }

        public IEnumerable<Token> Tokenize(CodeWalker walker, MiMFa.DaRQ.Compiler.Compiler? compiler = null)
        {
            if (compiler != null) this.Compiler = compiler;
            while (!walker.IsEnded)
                yield return TokenizeCode(walker);
        }

        protected abstract Token TokenizeCode(CodeWalker walker);
    }
}
