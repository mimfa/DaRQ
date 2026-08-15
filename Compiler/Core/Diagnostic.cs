// Converted from src/engine/DaRQ/Compiler/Core/Diagnostic.ts
using System;

namespace DaRQ.Compiler.Core
{
    public class Diagnostic
    {
        public string Id { get; }
        public Severity Severity { get; }
        public string Message { get; }
        public Range Range { get; }
        public string Source { get; }

        public Diagnostic(string id, Severity severity, string message, Range range = null, string source = null)
        {
            Id = id;
            Severity = severity;
            Message = message;
            Range = range;
            Source = source;
        }

        public bool HasLocation => Range != null;

        public override string ToString()
        {
            var location = Range != null ? $" ({Range})" : string.Empty;
            return $"[{Id}] {Severity}: {Message}{location}";
        }
    }
}
