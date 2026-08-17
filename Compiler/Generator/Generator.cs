// Converted from src/engine/DaRQ/Compiler/Generator/index.ts
using System;
using System.Collections.Generic;
using System.Linq;
using MiMFa.DaRQ.Compiler.Core;

namespace MiMFa.DaRQ.Compiler.Generator
{
    public abstract class Generator : IStage
    {
        public MiMFa.DaRQ.Compiler.Compiler? Compiler { get; set; }

        public object Transform(object input, MiMFa.DaRQ.Compiler.Compiler? compiler)
        {
            var program = (Node)input;
            var walker = new Assembler.NodeWalker(program.Children.ToArray(), compiler?.Input?.Source);
            return Generate(walker, compiler).ToArray();
        }

        public IEnumerable<string> Generate(Assembler.NodeWalker walker, MiMFa.DaRQ.Compiler.Compiler? compiler = null)
        {
            if (compiler != null) this.Compiler = compiler;
            while (!walker.IsEnded)
            {
                var c = GenerateCode(walker.Walk(), walker);
                if (c != null) yield return c;
            }
        }

        protected virtual string? GenerateCode(Node node, Assembler.NodeWalker walker)
        {
            string? code = null;
            if (node.Is(NodeType.Rule)) code = GenerateRuleCode(node, walker);
            else if (node.Is(NodeType.Procedure)) code = GenerateProcedureCode(node, walker);
            else if (node.Is(NodeType.Compute)) code = GenerateComputeCode(node, walker);
            else if (node.Is(NodeType.Plain)) code = GeneratePlainCode(node, walker);
            else if (node.Is(NodeType.Block)) code = GenerateBlockCode(node, walker);
            else if (node.Is(NodeType.Define)) code = GenerateDefineCode(node, walker);
            else if (node.Is(NodeType.Call)) code = GenerateCallCode(node, walker);
            else if (node.Is(NodeType.Helper)) code = GenerateHelperCode(node, walker);
            else if (node.Is(NodeType.Program)) code = GenerateProgramCode(node, walker);
            else if (!node.Is(NodeType.None)) return GenerateUnknownCode(node, walker);
            return code;
        }

        protected abstract string? GenerateProgramCode(Node node, Assembler.NodeWalker walker);
        protected abstract string? GenerateRuleCode(Node node, Assembler.NodeWalker walker);
        protected abstract string? GenerateProcedureCode(Node node, Assembler.NodeWalker walker);
        protected abstract string? GenerateComputeCode(Node node, Assembler.NodeWalker walker);
        protected abstract string? GeneratePlainCode(Node node, Assembler.NodeWalker walker);
        protected abstract string? GenerateBlockCode(Node node, Assembler.NodeWalker walker);
        protected abstract string? GenerateDefineCode(Node node, Assembler.NodeWalker walker);
        protected abstract string? GenerateCallCode(Node node, Assembler.NodeWalker walker);
        protected abstract string? GenerateHelperCode(Node node, Assembler.NodeWalker walker);
        protected abstract string? GenerateUnknownCode(Node node, Assembler.NodeWalker walker);
    }
}
