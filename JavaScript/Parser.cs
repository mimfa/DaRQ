using System.Collections.Generic;
using System.Linq;
using MiMFa.Compiler.Model;
using MiMFa.Compiler.Walker;

namespace MiMFa.Compiler.JavaScript
{
    public class Parser : MiMFa.Compiler.Parser.Parser
    {
        protected Node BasicParseToken(TokenWalker walker) => base.ParseToken(walker);
        protected IEnumerable<Node> BasicParseTokens(TokenWalker walker) => base.ParseTokens(walker);
        public int Locality { get; set; } = 0;

        protected virtual Node ParseTokenSingle(TokenWalker walker)
        {
            Locality++;
            var nodes = base.ParseTokens(walker).ToList();
            Locality--;
            if (nodes.Count > 1) return new Node(null, NodeType.Plain, nodes.ToArray());
            if (nodes.Count == 1) return nodes[0];
            return new Node();
        }

        protected override IEnumerable<Node> ParseTokens(TokenWalker walker)
        {
            foreach (var node in base.ParseTokens(walker)) yield return node;
            if (walker.Current != null)
            {
                var next = walker.PeekProcedure();
                if (next !=null && !next.Is(TokenType.EndScope) && (next.Is(TokenType.Symbol, TokenType.Facilitator) || next.IsMatch("[")))
                    foreach (var node in ParseTokens(walker)) yield return node;
            }
        }

        protected override IEnumerable<Node> ParseStatementToken(Token token, TokenWalker walker)
        {
            switch (token.Value)
            {
                case "if":
                    yield return new Node(token, NodeType.NormalSelector,
                        ParseTokenSingle(walker),
                        ParseToken(walker),
                        (walker.PeekProcedure()?.IsMatch("else") == true) ? ParseToken(walker) : null
                    );
                    break;
                case "else":
                    yield return ParseToken(walker);
                    break;

                case "switch":
                    yield return new Node(token, NodeType.LongSelector,
                        ParseTokenSingle(walker),
                        ParseToken(walker)
                    );
                    break;
                case "case":
                    var caseChildren = new List<Node> { ParseTokens(walker).First() };
                    caseChildren.AddRange(walker.MoveToProcedure().MapUntil(() => walker.Current == null || walker.Current.IsMatch("case", "default") || walker.Current.Is(TokenType.EndScope), () => ParseToken(walker)).ToList());
                    yield return new Node(token, NodeType.Procedure, caseChildren.ToArray());
                    break;
                case "default":
                    var defaultChildren = walker.MoveToProcedure().MapUntil(() => walker.Current == null || walker.Current.IsMatch("case", "default") || walker.Current.Is(TokenType.EndScope), () => ParseToken(walker)).ToList();
                    yield return new Node(token, NodeType.Procedure, defaultChildren.ToArray());
                    break;

                case "for":
                    yield return new Node(token, NodeType.ComputationIterator | NodeType.CollectionIterator,
                        ParseTokenSingle(walker), ParseToken(walker)
                    );
                    break;

                case "while":
                    yield return new Node(token, NodeType.ConditionIterator,
                        ParseTokenSingle(walker), ParseToken(walker)
                    );
                    break;

                case "do":
                    yield return new Node(token, NodeType.PostConditionIterator,
                        ParseToken(walker),
                        ParseTokenSingle(walker.MoveToProcedure())
                    );
                    break;

                case "break":
                case "continue":
                    yield return new Node(token, NodeType.Plain);
                    break;

                case "try":
                    yield return new Node(token, NodeType.Procedure, ParseToken(walker), ParseToken(walker));
                    break;
                case "catch":
                    yield return new Node(token, NodeType.Procedure,
                        walker.PeekProcedure()?.IsMatch("(") == true?ParseToken(walker): null,
                        ParseToken(walker),
                        walker.PeekProcedure()?.IsMatch("finally", "catch") == true ? ParseToken(walker) : null
                    );
                    break;
                case "finally":
                    yield return new Node(token, NodeType.Procedure,
                        ParseToken(walker),
                        walker.PeekProcedure()?.IsMatch("finally", "catch") == true ? ParseToken(walker) : null
                        );
                    break;

                case "return":
                case "yield":
                    yield return new Node(token, NodeType.Procedure, ParseToken(walker) );
                    break;

                case "throw":
                    yield return new Node(token, NodeType.Procedure, ParseToken(walker) );
                    break;

                case "import":
                case "export":
                    // support import { x as y } from '...'; and export default
                    if (walker.PeekProcedure()?.IsMatch("{", "*") == true)
                        yield return new Node(token, NodeType.Procedure,  ParseToken(walker));
                    else yield return new Node(token, NodeType.Procedure, ParseToken(walker) );
                    break;

                case "void":
                case "const":
                case "let":
                case "var":
                    yield return new Node(token, NodeType.Define,  ParseToken(walker) );
                    break;

                case "with":
                case "debugger":
                    yield return new Node(token, NodeType.Procedure,ParseToken(walker) );
                    break;

                case "class":
                    var nameNode = walker.PeekProcedure();
                    var classChildren = new List<Node>();
                    if (nameNode != null && nameNode.Is(TokenType.IdentifierKeyword))
                        classChildren.Add(ParseToken(walker));
                    if (walker.PeekProcedure()?.IsMatch("extends") == true) classChildren.Add(ParseToken(walker));
                    if (walker.PeekProcedure()?.Is(TokenType.StartScope) == true) classChildren.Add(ParseToken(walker));
                    yield return new Node(token, NodeType.Define, classChildren.ToArray());
                    break;

                case "delete":
                case "await":
                case "async":
                case "new":
                    yield return new Node(token, NodeType.Procedure, ParseToken(walker));
                    break;
            }
        }
        protected override IEnumerable<Node> ParseAccessToken(Token token, TokenWalker walker)
        {
            switch (token.Value)
            {
                case "private":
                    var n1 = ParseToken(walker);
                    n1.AccessType |= AccessType.Private;
                    yield return new Node(token, NodeType.Define, n1 );
                    break;
                case "protected":
                    var n2 = ParseToken(walker);
                    n2.AccessType |= AccessType.Protected;
                    yield return new Node(token, NodeType.Define,  n2 );
                    break;
                case "internal":
                    var n3 = ParseToken(walker);
                    n3.AccessType |= AccessType.Internal;
                    yield return new Node(token, NodeType.Define,  n3 );
                    break;
                case "public":
                    var n4 = ParseToken(walker);
                    n4.AccessType |= AccessType.Public;
                    yield return new Node(token, NodeType.Define, n4);
                    break;
                case "static":
                    var n5 = ParseToken(walker);
                    n5.AccessType |= AccessType.Global;
                    yield return new Node(token, NodeType.Define, n5);
                    break;
                default:
                    yield return new Node(token, NodeType.Define, ParseToken(walker) );
                    break;
            }
        }
        protected override IEnumerable<Node> ParseStructureToken(Token token, TokenWalker walker)
        {
            switch (token.Value)
            {
                case "get":
                case "set":
                case "function":
                    var t = walker.PeekProcedure();
                    if (t != null && t.Is(TokenType.Keyword))
                        yield return new Node(t.Clone(TokenType.FunctionKeyword, null, null), NodeType.Define,
                            ParseTokenSingle(walker).TrimSeparators(), ParseToken(walker)
                        );
                    else
                        yield return new Node(new Token(TokenType.FunctionKeyword, ""), NodeType.Define, 
                            ParseTokenSingle(walker).TrimSeparators(), ParseToken(walker)
                        );
                    break;
                case "implements":
                case "extends":
                case "interface":
                case "class":
                case "enum":
                case "package":
                default:
                    yield return new Node(token, NodeType.Define, ParseToken(walker), ParseToken(walker));
                    break;
            }
        }
        protected override IEnumerable<Node> ParseDataToken(Token token, TokenWalker walker)
        {
            if (token.Is(TokenType.PathData))
            {
                if (token.Is(TokenType.RegExPathData)) yield return new Node(token, NodeType.Plain);
                else yield return new Node(token.Clone(null, Newtonsoft.Json.JsonConvert.ToString(token.Value), null), NodeType.Plain);
            }
            else if (token.Is(TokenType.StringData))
            {
                if (token.Is(TokenType.TemplateStringData)) yield return new Node(token.Clone(null, "`" + token.Value + "`", null), NodeType.Plain);
                else yield return new Node(token.Clone(null, Newtonsoft.Json.JsonConvert.ToString(token.Value), null), NodeType.Plain);
            }
            else if (token.Is(TokenType.ArrayData)) yield return new Node(token.Clone(null, "[]", null), NodeType.Block);
            else if (token.Is(TokenType.ObjectData)) yield return new Node(token.Clone(null, "{}", null), NodeType.Block);
            else yield return new Node(token.Clone(null, token.Value.ToLower(), null), NodeType.Plain);
        }
        protected override IEnumerable<Node> ParseKeywordToken(Token token, TokenWalker walker)
        {
            var next = walker.PeekProcedure();
            if (next != null)
            {
                if (next.Is(TokenType.StartScope) && next.IsMatch("("))
                {
                    yield return new Node(token.Clone(TokenType.FunctionKeyword, null, null), NodeType.Call, ParseTokenSingle(walker).TrimSeparators().Children.ToArray());
                    yield break;
                }
                else if (next.Is(TokenType.Symbol))
                {
                    if (next.Is(TokenType.ConcatenatorSymbol))
                    {
                        yield return new Node(token.Clone(TokenType.NamespaceKeyword, null, null), NodeType.Plain, ParseToken(walker));
                        yield break;
                    }
                }
            }
            yield return new Node(token.Clone(TokenType.IdentifierKeyword, null, null), NodeType.Call);
        }
        protected override IEnumerable<Node> ParseSymbolToken(Token token, TokenWalker walker)
        {
            if (token.Is(TokenType.TerminatorSymbol))
                yield return new Node(token, NodeType.Plain);
            else if (token.Is(TokenType.SeparatorSymbol) == true)
                yield return new Node(token, NodeType.Procedure, ParseToken(walker));
            else if (token.IsMatch("!", "...") || token.Is(TokenType.ConcatenatorSymbol))
                yield return new Node(token, NodeType.Plain, ParseToken(walker));
            else if (token.IsMatch("=>", "?", "??") || token.Is(TokenType.OperatorSymbol))
                yield return new Node(token, NodeType.Compute, ParseTokenSingle(walker));
            else
            {
                yield return new Node(token, NodeType.Compute);
                yield return ParseTokenSingle(walker);
            }
        }
        protected override IEnumerable<Node> ParseScopeToken(Token token, TokenWalker walker)
        {
            if (token.Is(TokenType.StartScope))
            {
                var before = walker.PeekProcedure(-2);
                var tokenType = TokenType.Scope;
                if (token.IsMatch("{"))
                {
                    if (!(before != null && (before.Is(TokenType.EndScope) || before.Is(TokenType.TerminatorSymbol)))) tokenType = TokenType.ObjectData;
                    else tokenType = TokenType.Scope;
                }
                else if (token.IsMatch("[")) tokenType = TokenType.ArrayData;

                Locality++;
                var children = new List<Node>();
                while (walker.Current != null && !walker.Current.Is(TokenType.EndScope))
                {
                    var n = ParseToken(walker);
                    if (n != null && !n.Is(NodeType.None)) children.Add(n);
                }
                walker.Walk();
                Locality--;
                yield return new Node(token.Clone(tokenType, null, null), NodeType.Block, children.ToArray());
            }
            else if (token.Is(TokenType.EndScope))
                yield return new Node(token, NodeType.None);
            else
                yield return new Node(token, NodeType.Compute, ParseToken(walker));
        }
        protected override IEnumerable<Node> ParseFacilitatorToken(Token token, TokenWalker walker)
        {
            yield return new Node(token, NodeType.Procedure, ParseToken(walker).TrimSeparators());
        }
        protected override IEnumerable<Node> ParseCommentToken(Token token, TokenWalker walker)
        {
            bool isblock = token.Value.Contains("\n") || token.Value.StartsWith("/*");
            yield return new Node(new Token(TokenType.Unknown), isblock?NodeType.Procedure:NodeType.Block,
                new Node(token.Clone(Value: isblock ? token.Value : token.Value), NodeType.Helper),
                walker.Is(TokenType.EndScope) ? null : ParseToken(walker)
            );
        }
        protected override IEnumerable<Node> ParseUnknownToken(Token token, TokenWalker walker)
        {
            yield return new Node(token, NodeType.Unknown);
        }
    }
}
