using System;
using System.Collections.Generic;
using System.Linq;
using MiMFa.Compiler.Core;
using MiMFa.Compiler.Model;
using MiMFa.Compiler.Walker;

namespace MiMFa.Compiler.Generator
{
    public abstract class Generator : IStage
    {
        public virtual Compiler Compiler { get; set; }
        protected int Indention = 0;

        public virtual object Transform(object input, MiMFa.Compiler.Compiler compiler)
        {
            var program = (Node)input;
            var walker = new NodeWalker(program.Children.ToArray(), program.Token.Value??compiler?.Input?.Source);
            return string.Join(compiler.Options.MakeNewLine(Indention), Generate(walker, compiler));
        }

        public virtual bool Initialize(MiMFa.Compiler.Compiler compiler)
        {
            if (compiler != null) Compiler = compiler;
            return true;
        }
        public virtual IEnumerable<string> Generate(NodeWalker walker, MiMFa.Compiler.Compiler compiler = null)
        {
            if (!Initialize(compiler)) yield break;
            while (!walker.IsEnded)
            {
                var c = GenerateCode(walker.Walk(), walker);
                if (c != null) yield return c;
            }
        }

        protected virtual string GenerateCode(Node node, NodeWalker walker)
        {
            string code = null;
            if (node.Is(NodeType.Rule)) code = GenerateRuleCode(node, walker);
            if (code == null && node.Is(NodeType.Program)) code = GenerateProgramCode(node, walker);
            if (code == null && node.Is(NodeType.Section)) code = GenerateSectionCode(node, walker);
            if (code == null && node.Is(NodeType.Compute)) code = GenerateComputeCode(node, walker);
            if (code == null && node.Is(NodeType.Procedure)) code = GenerateProcedureCode(node, walker);
            if (code == null && node.Is(NodeType.Plain)) code = GeneratePlainCode(node, walker);
            if (code == null && node.Is(NodeType.Block)) code = GenerateBlockCode(node, walker);
            if (code == null && node.Is(NodeType.Define)) code = GenerateDefineCode(node, walker);
            if (code == null && node.Is(NodeType.Call)) code = GenerateCallCode(node, walker);
            if (code == null && node.Is(NodeType.Helper)) code = GenerateHelperCode(node, walker);
            if (code == null && !node.Is(NodeType.None)) code = GenerateUnknownCode(node, walker);
            return code;
        }

        protected abstract string GenerateProgramCode(Node node, NodeWalker walker);
        protected abstract string GenerateSectionCode(Node node, NodeWalker walker);
        protected abstract string GenerateComputeCode(Node node, NodeWalker walker);
        protected abstract string GenerateProcedureCode(Node node, NodeWalker walker);
        protected abstract string GeneratePlainCode(Node node, NodeWalker walker);
        protected abstract string GenerateRuleCode(Node node, NodeWalker walker);
        protected abstract string GenerateBlockCode(Node node, NodeWalker walker);
        protected abstract string GenerateDefineCode(Node node, NodeWalker walker);
        protected abstract string GenerateCallCode(Node node, NodeWalker walker);
        protected abstract string GenerateHelperCode(Node node, NodeWalker walker);
        protected abstract string GenerateUnknownCode(Node node, NodeWalker walker);
    }
}
