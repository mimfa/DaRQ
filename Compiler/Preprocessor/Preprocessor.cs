using System;
using System.Collections.Generic;
using System.Linq;
using MiMFa.Compiler.Core;
using MiMFa.Compiler.Model;
using MiMFa.Compiler.Walker;

namespace MiMFa.Compiler.Preprocessor
{
    public abstract class Preprocessor : IStage
    {
        public virtual Compiler Compiler { get; set; }

        public virtual bool Initialize(MiMFa.Compiler.Compiler compiler)
        {
            if (compiler != null) this.Compiler = compiler;
            return true;
        }
        public virtual object Transform(object input, MiMFa.Compiler.Compiler compiler)
        {
            Token[] tokens = input as Token[] ?? (input as IEnumerable<Token>)?.ToArray() ?? new Token[0];
            var walker = new TokenWalker(tokens, compiler?.Input?.Source);
            return Preprocess(walker, compiler).ToArray();
        }
        public virtual IEnumerable<Token> Preprocess(TokenWalker walker, MiMFa.Compiler.Compiler compiler = null)
        {
            if (!Initialize(compiler)) yield break;
            while (!walker.IsEnded)
                yield return PreprocessToken(walker.Walk(), walker);
        }

        public virtual Token PreprocessToken(Token token, TokenWalker walker) => token;
    }
}
