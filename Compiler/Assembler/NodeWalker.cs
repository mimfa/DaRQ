// Converted from src/engine/DaRQ/Compiler/Assembler/NodeWalker.ts
using System;
using DaRQ.Compiler.Core;

namespace DaRQ.Compiler.Assembler
{
    public class NodeWalker : DaRQ.Compiler.Core.WalkerBase<Node>
    {
        public NodeWalker(Node[] nodes, string source = null) : base(nodes, source) { }

        public bool Is(params NodeType[] nodeTypes)
        {
            if (Current != null) return Is(nodeTypes);
            return false;
        }

        public Node Peek(int offset = 0, params NodeType[] ofTypes)
        {
            Node p;
            int o = offset;
            while ((p = Peek(o)) != null)
            {
                if (ofTypes.Length == 0 || Array.Exists(ofTypes, v => (v & (p?.Type ?? 0)) != 0))
                    return p;
                o++;
            }
            return null;
        }
    }
}
