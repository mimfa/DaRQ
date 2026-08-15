// Converted from src/engine/DaRQ/Compiler/Parser/index.ts
using System;
using System.Collections.Generic;
using System.Linq;
using DaRQ.Compiler.Core;

namespace DaRQ.Compiler.Parser
{
    public abstract class Parser : IStage
    {
        public DaRQ.Compiler.Compiler Compiler { get; set; }

        public object Transform(object input, DaRQ.Compiler.Compiler compiler)
        {
            var tokens = (Token[])input;
            var walker = new TokenWalker(tokens, compiler?.Input?.Source);
            return Parse(walker, compiler).ToArray();
        }

        public IEnumerable<Node> Parse(TokenWalker walker, DaRQ.Compiler.Compiler compiler = null)
        {
            if (compiler != null) this.Compiler = compiler;
            while (!walker.IsEnded)
                yield return ParseToken(walker);
        }

        protected virtual Node ParseToken(TokenWalker walker)
        {
            var token = walker.Walk();
            Node newtoken = null;
            if (token.Is(TokenType.Statement))
            {
                newtoken = ParseStatementToken(token, walker);
                if (newtoken != null) return newtoken;
            }
            if (token.Is(TokenType.Access) && (newtoken = ParseAccessToken(token, walker)) != null) return newtoken;
            if (token.Is(TokenType.Structure) && (newtoken = ParseStructureToken(token, walker)) != null) return newtoken;
            if (token.Is(TokenType.Symbol) && (newtoken = ParseSymbolToken(token, walker)) != null) return newtoken;
            if (token.Is(TokenType.Scope) && (newtoken = ParseScopeToken(token, walker)) != null) return newtoken;
            if (token.Is(TokenType.Data) && (newtoken = ParseDataToken(token, walker)) != null) return newtoken;
            if (token.Is(TokenType.Facilitator) && (newtoken = ParseFacilitatorToken(token, walker)) != null) return newtoken;
            if (token.Is(TokenType.Keyword) && (newtoken = ParseKeywordToken(token, walker)) != null) return newtoken;
            if (token.Is(TokenType.Comment) && (newtoken = ParseCommentToken(token, walker)) != null) return newtoken;
            if (token.Is(TokenType.None)) return new Node { Token = token, Type = NodeType.None };
            return ParseUnknownToken(token, walker) ?? new Node { Token = token, Type = NodeType.Unknown };
        }

        protected abstract Node ParseStatementToken(Token token, TokenWalker walker);
        protected abstract Node ParseAccessToken(Token token, TokenWalker walker);
        protected abstract Node ParseStructureToken(Token token, TokenWalker walker);
        protected abstract Node ParseSymbolToken(Token token, TokenWalker walker);
        protected abstract Node ParseScopeToken(Token token, TokenWalker walker);
        protected abstract Node ParseDataToken(Token token, TokenWalker walker);
        protected abstract Node ParseFacilitatorToken(Token token, TokenWalker walker);
        protected abstract Node ParseKeywordToken(Token token, TokenWalker walker);
        protected abstract Node ParseCommentToken(Token token, TokenWalker walker);
        protected abstract Node ParseUnknownToken(Token token, TokenWalker walker);
    }
}
