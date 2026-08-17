// Converted from src/engine/DaRQ/Compiler/Assembler/NodeWalker.ts
using System;
using MiMFa.DaRQ.Compiler.Core;

namespace MiMFa.DaRQ.Compiler.Assembler
{
    public class NodeWalker : MiMFa.DaRQ.Compiler.Core.WalkerBase<Node>
    {
        public NodeWalker(Node[] nodes, string? source = null) : base(nodes, source) { }

        public bool Is(params NodeType[] nodeTypes)
        {
            var current = Current;
            if (current != null) return current.Is(nodeTypes);
            return false;
        }

        public Node? Peek(int offset = 0, params NodeType[] ofTypes)
        {
            Node? p;
            int o = offset;
            while ((p = Peek(o)) != null)
            {
                if (ofTypes.Length == 0 || Array.Exists(ofTypes, v => (v & (p.Type)) != 0))
                    return p;
                o++;
            }
            return null;
        }
    }
}
