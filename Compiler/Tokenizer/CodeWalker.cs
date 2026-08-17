// Converted from src/engine/DaRQ/Compiler/Tokenizer/CodeWalker.ts
using System;
using System.Linq;
using MiMFa.DaRQ.Compiler.Core;

namespace MiMFa.DaRQ.Compiler.Tokenizer
{
    public class CodeWalker : MiMFa.DaRQ.Compiler.Core.WalkerBase<char>
    {
        private int line = 1;
        private int column = 1;

        public Location Location => new Location(this.Position, this.line, this.column);

        public CodeWalker(string content, string? source = null) : base(content?.ToCharArray() ?? new char[0], source)
        {
        }

        public string PeekProcedure()
        {
            return string.Concat(PeekWhile(c => !char.IsWhiteSpace(c)).ToArray());
        }

        public override char Walk()
        {
            var ch = base.Walk();
            if (ch == '\n')
            {
                line++;
                column = 1;
            }
            else column++;
            return ch;
        }

        public string WalkProcedure()
        {
            return new string(WalkWhile(c => !char.IsWhiteSpace(c)).ToArray());
        }

        public string WalkToProcedure()
        {
            return new string(WalkWhile(c => char.IsWhiteSpace(c)).ToArray());
        }

        public CodeWalker MoveToProcedure()
        {
            WalkToProcedure();
            return this;
        }

        public bool StartsWith(string text)
        {
            for (int i = 0; i < text.Length; i++) if (Peek(i) != text[i]) return false;
            return true;
        }

        public string Remaining(int length = int.MaxValue)
        {
            var result = string.Empty;
            for (int i = Position; i < Math.Min(Position + length, Length); i++) result += Peek(i - Position);
            return result;
        }
    }
}
