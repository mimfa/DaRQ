using System.Collections.Generic;
using MiMFa.Compiler.Model;
using MiMFa.Compiler.Walker;

namespace MiMFa.Compiler.DaRQ
{
    /// <summary>JavaScript token-to-independent-node parser.</summary>
    public class Parser : MiMFa.Compiler.JavaScript.Parser
    {
        protected override IEnumerable<Node> ParseStatementToken(Token token, TokenWalker walker) { yield break; }
        protected override IEnumerable<Node> ParseScopeToken(Token token, TokenWalker walker) { yield break; }
        protected override IEnumerable<Node> ParseDataToken(Token token, TokenWalker walker) { yield break; }
        protected override IEnumerable<Node> ParseSymbolToken(Token token, TokenWalker walker) { yield break; }
        protected override IEnumerable<Node> ParseKeywordToken(Token token, TokenWalker walker) { yield break; }
        protected override IEnumerable<Node> ParseCommentToken(Token token, TokenWalker walker) { yield break; }
        protected override IEnumerable<Node> ParseStartToken(Token token, TokenWalker walker) { yield break; }
        protected override IEnumerable<Node> ParsePrefixToken(Token token, TokenWalker walker) { yield break; }
        protected override IEnumerable<Node> ParseMiddleToken(Token token, TokenWalker walker) { yield break; }
        protected override IEnumerable<Node> ParseSuffixToken(Token token, TokenWalker walker) { yield break; }
        protected override IEnumerable<Node> ParseEndToken(Token token, TokenWalker walker) { yield break; }
        protected override IEnumerable<Node> ParseUnknownToken(Token token, TokenWalker walker) { yield break; }
    }
}
