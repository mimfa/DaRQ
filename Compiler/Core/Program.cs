// Converted from src/engine/DaRQ/Compiler/Core/Program.ts
using System;

namespace MiMFa.DaRQ.Compiler.Core
{
    public class Program : Node
    {
        public Program(string? source = null, params Node?[] children) : base(new Token(TokenType.PathData, source), NodeType.Program, children: children)
        {
        }
    }
}