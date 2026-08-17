// Converted from src/engine/DaRQ/Compiler/Core/Token.ts
using System;

namespace MiMFa.DaRQ.Compiler.Core
{
    public class Token
    {
        public string Value { get; set; }
        public TokenType Type { get; set; }
        public Location Location { get; set; }

        public Token(TokenType? type = null, string? value = null, Location? location = null)
        {
            Value = value ?? string.Empty;
            Type = type ?? (string.IsNullOrEmpty(value) ? TokenType.None : TokenType.Unknown);
            Location = location ?? new Location();
        }

        public Token Update(TokenType? Type = null, string? Value = null, Location? Location = null)
        {
            this.Value = Value ?? this.Value;
            this.Type = Type ?? this.Type;
            this.Location = Location ?? this.Location;
            return this;
        }

        public Token Clone(TokenType? Type = null, string? Value = null, Location? Location = null)
        {
            return new Token(Type ?? this.Type, Value ?? this.Value, Location ?? this.Location);
        }

        public bool Is(params TokenType[] tokenTypes)
        {
            foreach (var tokenType in tokenTypes)
                if ((int)(this.Type & tokenType) == (int)tokenType)
                    return true;
            return false;
        }

        public bool IsProcedure() => !Is(TokenType.None, TokenType.Comment);
        public bool IsIndependent() => Is(TokenType.Statement, TokenType.Access);
        public bool IsDependent() => IsMatch("[", "(") || Is(TokenType.ConcatenatorSymbol, TokenType.OperatorSymbol, TokenType.Data, TokenType.Facilitator) && !IsMatch("}", ")");

        public bool IsMatch(params string[] values)
        {
            foreach (var v in values)
                if (string.Equals(this.Value, v, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        public double Sameness(string value)
        {
            if (this.Value == value) return 1.0;
            if (string.Equals(this.Value, value, StringComparison.OrdinalIgnoreCase)) return 0.5;
            return 0.0;
        }
    }
}
