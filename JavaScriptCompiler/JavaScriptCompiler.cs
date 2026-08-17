// Converted from src/engine/DaRQ/JavaScript-Compiler/index.ts
using System;
using MiMFa.DaRQ.Compiler;

namespace MiMFa.DaRQ.JavaScriptCompiler
{
    public class JavaScriptCompiler : MiMFa.DaRQ.Compiler.Compiler
    {
        public JavaScriptCompiler(Options? options = null) : base(new IStage[] {
            new Tokenizer(),
            new Preprocessor(),
            new Parser(),
            new Assembler(),
            new Executor()
        }, options ?? new Options())
        {
        }
    }
}
