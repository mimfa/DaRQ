// Converted from src/engine/DaRQ/Compiler/Core/Range.ts
using System;

namespace DaRQ.Compiler.Core
{
    public class Range
    {
        public Location Start { get; }
        public Location End { get; }

        public Range(Location start, Location end)
        {
            Start = start;
            End = end;
        }

        public int Length => End.Index - Start.Index;

        public bool Contains(Location location) => location.Index >= Start.Index && location.Index <= End.Index;

        public bool Overlaps(Range range) => Start.Index <= range.End.Index && End.Index >= range.Start.Index;

        public override string ToString() => $"{Start}-{End}";
    }
}
