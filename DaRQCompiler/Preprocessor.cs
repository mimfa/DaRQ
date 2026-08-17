// Converted from src/engine/DaRQ/DaRQ-Compiler/Preprocessor.ts
using System;

namespace MiMFa.DaRQ.DaRQCompiler
{
    public class Preprocessor : MiMFa.DaRQ.JavaScriptCompiler.Preprocessor
    {
        // No additional behavior beyond JavaScript preprocessor for now.
        public new DaRQCompiler? Compiler { get; set; }

        public object Transform(object input, DaRQCompiler? compiler)
        {
            return base.Transform(input, compiler);
        }
    }
}
