// Converted from src/engine/DaRQ/JavaScript-Compiler/Assembler.ts
using System;
using DaRQ.Compiler;
using DaRQ.Compiler.Core;
using DaRQ.Compiler.Assembler;

namespace DaRQ.JavaScriptCompiler
{
    public class Assembler : DaRQ.Compiler.Assembler.Assembler
    {
        protected override Node AssembleNode(Node node, DaRQ.Compiler.Assembler.NodeWalker walker)
        {
            // Original TypeScript simply returned the node as-is.
            return node;
        }
    }
}
