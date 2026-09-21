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

        protected virtual Node AssembleNode(Node node, NodeWalker walker)
        {
            Node latest = null;
            if (latest == null && node.Is(NodeType.Structure))
                latest = AssembleStructureNode(node, walker);
            if (latest == null && node.Is(NodeType.Program))
                latest = AssembleProgramNode(node, walker);
            if (latest == null && node.Is(NodeType.Region))
                latest = AssembleRegionNode(node, walker);
            if (latest == null && node.Is(NodeType.Line))
                latest = AssembleLineNode(node, walker);
            if (latest == null && node.Is(NodeType.Chunk))
                latest = AssembleChunkNode(node, walker);

            if (latest == null && node.Is(NodeType.Independ))
                latest = AssembleIndependNode(node, walker);
            if (latest == null && node.Is(NodeType.Depend))
                latest = AssembleDependNode(node, walker);
            if (latest == null && node.Is(NodeType.Append))
                latest = AssembleAppendNode(node, walker);
            if (latest == null && node.Is(NodeType.Prepend))
                latest = AssemblePrependNode(node, walker);

            if (latest == null && node.Is(NodeType.None))
                latest = node.Update(type: NodeType.None);
            return latest??AssembleUnknownNode(node, walker);
        }

        protected abstract Node AssembleStructureNode(Node node, NodeWalker walker);
        protected abstract Node AssembleProgramNode(Node node, NodeWalker walker);
        protected abstract Node AssembleRegionNode(Node node, NodeWalker walker);
        protected abstract Node AssembleLineNode(Node node, NodeWalker walker);
        protected abstract Node AssembleChunkNode(Node node, NodeWalker walker);

        protected abstract Node AssembleIndependNode(Node node, NodeWalker walker);
        protected abstract Node AssembleDependNode(Node node, NodeWalker walker);
        protected abstract Node AssembleAppendNode(Node node, NodeWalker walker);
        protected abstract Node AssemblePrependNode(Node node, NodeWalker walker);

        protected abstract Node AssembleUnknownNode(Node node, NodeWalker walker);
    }
}
