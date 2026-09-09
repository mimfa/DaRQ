using System;
using System.Collections.Generic;
using System.Linq;
using MiMFa.Compiler.Core;
using MiMFa.Compiler.Model;
using MiMFa.Compiler.Walker;

namespace MiMFa.Compiler.Tokenizer
{
    public abstract class Tokenizer : IStage
    {
        public virtual Compiler Compiler { get; set; }

        public virtual bool Initialize(MiMFa.Compiler.Compiler compiler)
        {
            if (compiler != null) this.Compiler = compiler;
            return true;
        }
        public virtual object Transform(object input, MiMFa.Compiler.Compiler compiler)
        {
            var walker = new CodeWalker((string)input, compiler?.Input?.Source);
            return Tokenize(walker, compiler).ToArray();
        }
        public virtual IEnumerable<Token> Tokenize(CodeWalker walker, MiMFa.Compiler.Compiler compiler = null)
        {
            if (!Initialize(compiler)) yield break;
            while (!walker.IsEnded)
                yield return TokenizeCode(walker);
        }

        protected abstract Token TokenizeCode(CodeWalker walker);
    }
}
