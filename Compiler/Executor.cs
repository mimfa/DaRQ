// Base Executor class for compiler stages
using System;
using MiMFa.DaRQ.Compiler.Core;

namespace MiMFa.DaRQ.Compiler
{
    public abstract class Executor : IStage
    {
        public MiMFa.DaRQ.Compiler.Compiler? Compiler { get; set; }

        protected virtual string ExecuteCode(Node? node)
        {
            return node?.Token?.Value??"";
        }

        public object Transform(object input, MiMFa.DaRQ.Compiler.Compiler compiler)
        {
            this.Compiler = compiler;
            var node = input as Node;
            return ExecuteCode(node);
        }
    }
}
