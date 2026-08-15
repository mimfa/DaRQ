// Converted from src/engine/DaRQ/Compiler/Output.ts
using System;

namespace DaRQ.Compiler
{
    public class Output
    {
        public string Content { get; }
        public string Source { get; }
        public string[] Errors { get; }

        public Output(string content = "", string source = null, string[] errors = null)
        {
            Content = content;
            Source = source;
            Errors = errors ?? Array.Empty<string>();
        }
    }
}
