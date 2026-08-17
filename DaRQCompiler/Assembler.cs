// Converted from src/engine/DaRQ/DaRQ-Compiler/Assembler.ts
using System;

namespace MiMFa.DaRQ.DaRQCompiler
{
    public class Assembler : MiMFa.DaRQ.JavaScriptCompiler.Assembler
    {
        // DaRQ-specific assembler inherits JavaScript assembler behavior.
        public new DaRQCompiler? Compiler { get; set; }

        public object Transform(object input, DaRQCompiler? compiler)
        {
            return base.Transform(input, compiler);
        }

    }
}
