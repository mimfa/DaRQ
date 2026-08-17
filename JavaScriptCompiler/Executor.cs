// Converted from src/engine/DaRQ/JavaScript-Compiler/Executor.ts
using System;
using MiMFa.DaRQ.Compiler.Core;

namespace MiMFa.DaRQ.JavaScriptCompiler
{
    public class Executor : MiMFa.DaRQ.Compiler.Executor
    {
        protected override string ExecuteCode(Node? node)
        {
            return node?.Token?.Value??"";
        }
    }
}
