// Converted from src/engine/DaRQ/Compiler/Output.ts
using MiMFa.Compiler.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiMFa.Compiler
{
    public class Output
    {
        public string Content { get; set; }
        public string Source { get; private set; }
        public List<string> Errors { get; } = new List<string>();

        public Output(string source = null)
        {
            Source = source;
        }

        public Output Error(string message = "")
        {
            Errors.Add(message);
            return this;
        }
        public Output Error(Exception ex)
        {
            return Error(ex.Message);
        }
    }
}
