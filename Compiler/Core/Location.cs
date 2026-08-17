// Converted from src/engine/DaRQ/Compiler/Core/Location.ts
using System;

namespace MiMFa.DaRQ.Compiler.Core
{
    public class Location
    {
        public int Index { get; }
        public int Line { get; }
        public int Column { get; }

        public Location(int index = 0, int line = 1, int column = 1)
        {
            Index = index;
            Line = line;
            Column = column;
        }

        public Location Move(int length, string text = "")
        {
            var line = Line;
            var column = Column;
            foreach (var ch in text)
            {
                if (ch == '\n')
                {
                    line++;
                    column = 1;
                }
                else column++;
            }
            return new Location(Index + length, line, column);
        }

        public bool Equals(Location location)
        {
            return location != null && Index == location.Index && Line == location.Line && Column == location.Column;
        }

        public override string ToString() => $"{Line}:{Column}";
    }
}
