using System;
using System.Collections.Generic;
using System.Linq;
using MiMFa.Compiler.Core;
using MiMFa.Compiler.Model;
using MiMFa.Compiler.Walker;

namespace MiMFa.Compiler.Parser
{
    public abstract class Parser : IStage
    {
        public virtual Compiler Compiler { get; set; }

        public virtual bool Initialize(MiMFa.Compiler.Compiler compiler)
        {
            if (compiler != null) this.Compiler = compiler;
            return true;
        }
        public virtual object Transform(object input, Compiler compiler)
        {
            var tokens = (Token[])input;
            var walker = new TokenWalker(tokens, compiler?.Input?.Source);
            return Parse(walker, compiler).ToArray();
        }
        public virtual IEnumerable<Node> Parse(TokenWalker walker, Compiler compiler = null)
        {
            if (!Initialize(compiler)) yield break;
            while (!walker.IsEnded)
                yield return ParseToken(walker);
        }

        protected virtual Node ParseToken(TokenWalker walker)
        {
            var nodes = ParseTokens(walker).ToList();
            if (nodes.Count > 1) return new Node(null, NodeType.Plain, nodes.ToArray());
            if (nodes.Count == 1) return nodes[0];
            return new Node();
        }
        protected virtual IEnumerable<Node> ParseTokens(TokenWalker walker)
        {
            var token = walker.Walk();
            Node latest = null;
            if (latest == null && token.Is(TokenType.Statement))
                foreach (var node in ParseStatementToken(token, walker))
                    yield return latest = node;
            if (latest == null && token.Is(TokenType.Access))
                foreach (var node in ParseAccessToken(token, walker))
                    yield return latest = node;
            if (latest == null && token.Is(TokenType.Structure))
                foreach (var node in ParseStructureToken(token, walker))
                    yield return latest = node;
            if (latest == null && token.Is(TokenType.Symbol))
                foreach (var node in ParseSymbolToken(token, walker))
                    yield return latest = node;
            if (latest == null && token.Is(TokenType.Scope))
                foreach (var node in ParseScopeToken(token, walker))
                    yield return latest = node;
            if (latest == null && token.Is(TokenType.Data))
                foreach (var node in ParseDataToken(token, walker))
                    yield return latest = node;
            if (latest == null && token.Is(TokenType.Facilitator))
                foreach (var node in ParseFacilitatorToken(token, walker))
                    yield return latest = node;
            if (latest == null && token.Is(TokenType.Keyword))
                foreach (var node in ParseKeywordToken(token, walker))
                    yield return latest = node;
            if (latest == null && token.Is(TokenType.Comment))
                foreach (var node in ParseCommentToken(token, walker))
                    yield return latest = node;
            if (latest == null && token.Is(TokenType.None))
                yield return new Node { Token = token, Type = NodeType.None };
            if (latest == null)
                foreach (var node in ParseUnknownToken(token, walker))
                    yield return latest = node;
            if (latest == null) yield return new Node { Token = token, Type = NodeType.Unknown };
        }

        protected abstract IEnumerable<Node> ParseStatementToken(Token token, TokenWalker walker);
        protected abstract IEnumerable<Node> ParseAccessToken(Token token, TokenWalker walker);
        protected abstract IEnumerable<Node> ParseStructureToken(Token token, TokenWalker walker);
        protected abstract IEnumerable<Node> ParseSymbolToken(Token token, TokenWalker walker);
        protected abstract IEnumerable<Node> ParseScopeToken(Token token, TokenWalker walker);
        protected abstract IEnumerable<Node> ParseDataToken(Token token, TokenWalker walker);
        protected abstract IEnumerable<Node> ParseFacilitatorToken(Token token, TokenWalker walker);
        protected abstract IEnumerable<Node> ParseKeywordToken(Token token, TokenWalker walker);
        protected abstract IEnumerable<Node> ParseCommentToken(Token token, TokenWalker walker);
        protected abstract IEnumerable<Node> ParseUnknownToken(Token token, TokenWalker walker);
    }
}
