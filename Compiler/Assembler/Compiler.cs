using MiMFa.Compiler.Model;
using MiMFa.Compiler.Walker;
using System.Collections.Generic;
using System.Linq;

namespace MiMFa.Compiler.Assembler
{
    public abstract class Assembler : IStage
    {
        public virtual Compiler Compiler { get; set; }

        public virtual bool Initialize(MiMFa.Compiler.Compiler compiler)
        {
            if (compiler != null) this.Compiler = compiler;
            return true;
        }
        public virtual object Transform(object input, MiMFa.Compiler.Compiler compiler)
        {
            var walker = new NodeWalker((Node[])input, compiler?.Input?.Source);
            return new Program(compiler?.Input?.Source, Assemble(walker, compiler).ToArray());
        }

        public virtual IEnumerable<Node> Assemble(NodeWalker walker, MiMFa.Compiler.Compiler compiler = null)
        {
            if (!Initialize(compiler)) yield break;
            while (!walker.IsEnded)
                yield return AssembleNode(walker.Walk(), walker);
        }

        protected abstract Node AssembleNode(Node node, NodeWalker walker);
    }
}
