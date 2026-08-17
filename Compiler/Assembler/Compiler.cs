// Converted from src/engine/DaRQ/Compiler/Assembler/index.ts
using System;
using System.Collections.Generic;
using System.Linq;
using MiMFa.DaRQ.Compiler.Core;

namespace MiMFa.DaRQ.Compiler.Assembler
{
    public abstract class Assembler : IStage
    {
        public MiMFa.DaRQ.Compiler.Compiler? Compiler { get; set; }

        public object Transform(object input, MiMFa.DaRQ.Compiler.Compiler? compiler)
        {
            var tokens = (Node[])input;
            var walker = new NodeWalker(tokens, compiler?.Input?.Source);
            return Assemble(walker, compiler).ToArray();
        }

        public IEnumerable<Node> Assemble(NodeWalker walker, MiMFa.DaRQ.Compiler.Compiler? compiler = null)
        {
            if (compiler != null) this.Compiler = compiler;
            while (!walker.IsEnded)
                yield return AssembleNode(walker.Walk(), walker);
        }

        protected abstract Node AssembleNode(Node node, NodeWalker walker);
    }
}
