// Converted from src/engine/DaRQ/JavaScript-Compiler/Executor.ts
using System;
using DaRQ.Compiler.Core;

namespace DaRQ.JavaScriptCompiler
{
    public class Executor : Compiler.Executor
    {
        protected override string ExecuteCode(Node node)
        {
            return node.Token.Value;
        }
    }
}
