// Converted from src/engine/DaRQ/JavaScript-Compiler/index.ts
using System;
using DaRQ.Compiler;

namespace DaRQ.JavaScriptCompiler
{
    public class JavaScriptCompiler : Compiler
    {
        public JavaScriptCompiler(Options options = null) : base(new IStage[] {
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
