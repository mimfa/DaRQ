// Converted from src/engine/DaRQ/Compiler/Options.ts
using System;

namespace DaRQ.Compiler
{
    public class Options
    {
        public bool Strict { get; set; } = true;
        public bool Injection { get; set; } = true;
        public bool Optimize { get; set; } = true;
        public bool GenerateSourceMap { get; set; } = false;
        public string Escape { get; set; } = "\\";
        public string WarpSeparator { get; set; } = " ";
        public string LineSeparator { get; set; } = "\n";

        public string MakeIndention(int indentions) => new string('\t', indentions);
        public string MakeNewLine(int indentions) => LineSeparator + MakeIndention(indentions);
    }
}
