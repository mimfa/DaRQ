using MiMFa.Compiler.Core;
using MiMFa.Compiler.Model;
using System;

namespace MiMFa.Compiler.Executor
{
    public abstract class Executor : IStage
    {
        public virtual Compiler Compiler { get; set; }

        public virtual bool Initialize(MiMFa.Compiler.Compiler compiler)
        {
            if (compiler != null) this.Compiler = compiler;
            return true;
        }
        public virtual object Transform(object input, MiMFa.Compiler.Compiler compiler)
        {
            if (!Initialize(compiler)) return null;
            var node = input as Node;
            return ExecuteCode(node);
        }
        protected virtual string ExecuteCode(Node node)
        {
            return node?.Token?.Value ?? "";
        }
    }
}
