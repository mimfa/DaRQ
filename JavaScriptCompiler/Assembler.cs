// Converted from src/engine/DaRQ/JavaScript-Compiler/Assembler.ts
using System;
using MiMFa.DaRQ.Compiler;
using MiMFa.DaRQ.Compiler.Core;
using MiMFa.DaRQ.Compiler.Assembler;

namespace MiMFa.DaRQ.JavaScriptCompiler
{
    public class Assembler : MiMFa.DaRQ.Compiler.Assembler.Assembler
    {
        protected override Node AssembleNode(Node node, MiMFa.DaRQ.Compiler.Assembler.NodeWalker walker)
        {
            // Original TypeScript simply returned the node as-is.
            return node;
        }
    }
}
