using System;
using System.Collections.Generic;
using System.Linq;
using MiMFa.Compiler.Model;
using MiMFa.Compiler.Walker;

namespace MiMFa.Compiler.JavaScript
{
    public class Parser : MiMFa.Compiler.Parser.Parser
    {
        public int Locality { get; set; } = 0;

        public override IEnumerable<Node> Parse(TokenWalker walker, MiMFa.Compiler.Compiler compiler = null)
        {
            if (!Initialize(compiler)) yield break;
            Locality = 0;
            while (!walker.IsEnded)
                yield return BlockParseToken(walker);
        }

        protected virtual Node BlockParseToken(TokenWalker walker)
        {
            Locality++;
            var nodes = BlockParseTokens(walker).ToList();
            Locality--;
            if (nodes.Count > 1) return new Node(null, NodeType.Plain, Locality + 1, nodes.ToArray());
            if (nodes.Count == 1) return nodes[0].Update(location: Locality + 1);
            return new Node(location: Locality + 1);
        }
        protected virtual IEnumerable<Node> BlockParseTokens(TokenWalker walker)
        {
            foreach (var node in SequenceParseTokens(walker)) yield return node;
            if (walker.Current != null)
            {
                var next = walker.PeekProcedure();
                if (next != null && !next.Is(TokenType.End | TokenType.Scope))
                    if (next.Is(
                            TokenType.Symbol
                        )
                    )
                        foreach (var node in BlockParseTokens(walker))
                            yield return node;
            }
        }

        protected virtual Node SequenceParseToken(TokenWalker walker)
        {
            Locality++;
            var nodes = SequenceParseTokens(walker).ToList();
            Locality--;
            if (nodes.Count > 1) return new Node(null, NodeType.Plain, Locality + 1,nodes.ToArray());
            if (nodes.Count == 1) return nodes[0].Update(location:Locality +1);
            return new Node(location: Locality + 1);
        }
        protected virtual IEnumerable<Node> SequenceParseTokens(TokenWalker walker)
        {
            foreach (var node in CompactParseTokens(walker)) yield return node;
            if (walker.Current != null)
            {
                var next = walker.PeekProcedure();
                if (next != null && !next.Is(TokenType.End | TokenType.Scope))
                    if (next.Is(
                            TokenType.DelimiterSymbol
                        )
                    )
                    foreach (var node in SequenceParseTokens(walker))
                            yield return node;
            }
        }

        protected virtual Node CompactParseToken(TokenWalker walker)
        {
            var nodes = CompactParseTokens(walker).ToList();
            if (nodes.Count > 1) return new Node(null, NodeType.Plain, Locality, nodes.ToArray());
            if (nodes.Count == 1) return nodes[0].Update(location: Locality);
            return new Node(location: Locality);
        }
        protected virtual IEnumerable<Node> CompactParseTokens(TokenWalker walker)
        {
            foreach (var node in ParseTokens(walker)) yield return node;
            if (walker.Current != null)
            {
                var next = walker.PeekProcedure();
                if (next != null && !next.Is(TokenType.End | TokenType.Scope))
                    if (next.Is(
                            TokenType.ConcatenatorSymbol,
                            TokenType.Suffix
                        )
                    )
                        yield return CompactParseToken(walker);
                    else if (next.Is(
                            TokenType.Middle,
                            TokenType.End,
                            TokenType.Start | TokenType.Scope
                        )
                    )
                        foreach (var node in CompactParseTokens(walker))
                            yield return node;
            }
        }

        protected virtual Node SingleParseToken(TokenWalker walker)
        {
            return ParseToken(walker).Update(location: Locality);
        }


        protected override IEnumerable<Node> ParseStructureToken(Token token, TokenWalker walker)
        {
            switch (token.Value)
            {
                case "get":
                case "set":
                case "function":
                    var fname = walker.PeekProcedure();
                    if (fname != null && fname.Is(TokenType.Keyword))
                        yield return (Compiler as Compiler).SetKeyword(fname.Value, new Node(fname.Clone(TokenType.FunctionKeyword), NodeType.Define | NodeType.Section,
                            SingleParseToken(walker), SingleParseToken(walker)
                        ));
                    else
                        yield return new Node(new Token(TokenType.FunctionKeyword, ""), NodeType.Define | NodeType.Section,
                            SingleParseToken(walker), SingleParseToken(walker)
                        );
                    break;

                case "implements":
                case "extends":
                    yield return new Node(token, NodeType.Define, SingleParseToken(walker), SingleParseToken(walker));
                    break;

                case "interface":
                case "class":
                case "enum":
                case "package":
                default:
                    var cname = walker.PeekProcedure();
                    if (cname.Is(TokenType.Keyword))
                        yield return (Compiler as Compiler).SetKeyword(cname.Value, new Node(token, NodeType.Define | NodeType.Section, new Node(walker.Walk(), NodeType.Plain), SingleParseToken(walker)));
                    else yield return new Node(token, NodeType.Define | NodeType.Section, SingleParseToken(walker));
                    break;

                case "if":
                    yield return new Node(token, NodeType.NormalSelector,
                        SingleParseToken(walker),
                        BlockParseToken(walker),
                        (walker.PeekProcedure()?.IsMatch("else") == true) ? BlockParseToken(walker) : null
                    );
                    break;
                case "else":
                    yield return BlockParseToken(walker);
                    break;

                case "switch":
                    yield return new Node(token, NodeType.LongSelector | NodeType.Section,
                        SingleParseToken(walker),
                        BlockParseToken(walker)
                    );
                    break;
                case "case":
                    var caseChildren = new List<Node> { SingleParseToken(walker) };
                    caseChildren.AddRange(walker.MoveToProcedure().MapUntil(() => walker.Current == null || walker.Current.IsMatch("case", "default") || walker.Current.Is(TokenType.End | TokenType.Scope), () => BlockParseToken(walker)).ToList());
                    yield return new Node(token, NodeType.Procedure, caseChildren.ToArray());
                    break;
                case "default":
                    var defaultChildren = walker.MoveToProcedure().MapUntil(() => walker.Current == null || walker.Current.IsMatch("case", "default") || walker.Current.Is(TokenType.End | TokenType.Scope), () => BlockParseToken(walker)).ToList();
                    yield return new Node(token, NodeType.Procedure, defaultChildren.ToArray());
                    break;

                case "for":
                    yield return new Node(token, NodeType.ComputationIterator | NodeType.CollectionIterator,
                        SingleParseToken(walker), BlockParseToken(walker)
                    );
                    break;

                case "while":
                    yield return new Node(token, NodeType.ConditionIterator,
                        SingleParseToken(walker), BlockParseToken(walker)
                    );
                    break;

                case "do":
                    yield return new Node(token, NodeType.PostConditionIterator,
                        SingleParseToken(walker),
                        SingleParseToken(walker.MoveToProcedure())
                    );
                    break;

                case "break":
                case "continue":
                    yield return new Node(token, NodeType.Plain);
                    break;

                case "try":
                    yield return new Node(token, NodeType.Procedure, SingleParseToken(walker), SingleParseToken(walker));
                    break;
                case "catch":
                    yield return new Node(token, NodeType.Procedure,
                        walker.PeekProcedure()?.IsMatch("(") == true? SingleParseToken(walker): null,
                        SingleParseToken(walker),
                        walker.PeekProcedure()?.IsMatch("finally", "catch") == true ? SingleParseToken(walker) : null
                    );
                    break;
                case "finally":
                    yield return new Node(token, NodeType.Procedure,
                        SingleParseToken(walker),
                        walker.PeekProcedure()?.IsMatch("finally", "catch") == true ? SingleParseToken(walker) : null
                        );
                    break;

                case "return":
                case "yield":
                case "throw":
                    yield return new Node(token, NodeType.Procedure, BlockParseToken(walker));
                    break;

                case "import":
                case "export":
                    // support import { x as y } from '...'; and export default
                    if (walker.PeekProcedure()?.IsMatch("{", "*") == true)
                        yield return new Node(token, NodeType.Procedure, BlockParseToken(walker));
                    else yield return new Node(token, NodeType.Procedure, BlockParseToken(walker) );
                    break;

                case "void":
                    yield return new Node(token, NodeType.Define, SingleParseToken(walker) );
                    break;

                case "var":
                case "let":
                case "const":
                    yield return new Node(token, NodeType.Procedure, BlockParseToken(walker));
                    break;

                case "with":
                case "debugger":
                    yield return new Node(token, NodeType.Procedure, BlockParseToken(walker) );
                    break;

                case "delete":
                    yield return new Node(token, NodeType.Procedure, BlockParseToken(walker));
                    break;

                case "await":
                case "async":
                case "new":
                    yield return new Node(token, NodeType.Procedure, CompactParseToken(walker));
                    break;

                case "private":
                    var n1 = BlockParseToken(walker);
                    n1.AccessType |= AccessType.Private;
                    yield return new Node(token, NodeType.Define, n1);
                    break;
                case "protected":
                    var n2 = BlockParseToken(walker);
                    n2.AccessType |= AccessType.Protected;
                    yield return new Node(token, NodeType.Define, n2);
                    break;
                case "internal":
                    var n3 = BlockParseToken(walker);
                    n3.AccessType |= AccessType.Internal;
                    yield return new Node(token, NodeType.Define, n3);
                    break;
                case "public":
                    var n4 = BlockParseToken(walker);
                    n4.AccessType |= AccessType.Public;
                    yield return new Node(token, NodeType.Define, n4);
                    break;
                case "static":
                    var n5 = BlockParseToken(walker);
                    n5.AccessType |= AccessType.Global;
                    yield return new Node(token, NodeType.Define, n5);
                    break;
            }
        }
        protected override IEnumerable<Node> ParseDataToken(Token token, TokenWalker walker)
        {
            if (token.Is(TokenType.StringData))
            {
                if (token.Is(TokenType.PatternData)) yield return new Node(token, NodeType.Plain);
                else if (token.Is(TokenType.TemplateStringData)) yield return new Node(token.Clone(null, "`" + token.Value + "`", null), NodeType.Plain);
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
                if (next.Is(TokenType.Start | TokenType.Scope) && next.IsMatch("("))
                {
                    yield return new Node(token.Clone(TokenType.FunctionKeyword, null, null), NodeType.Call, SingleParseToken(walker).Children.ToArray());
                    yield break;
                }
                else if (next.Is(TokenType.ConcatenatorSymbol))
                {
                    yield return new Node(token.Clone(TokenType.NamespaceKeyword, null, null), NodeType.Plain, CompactParseToken(walker));
                    yield break;
                }
            }
            yield return new Node(token.Clone(TokenType.IdentifierKeyword, null, null), NodeType.Call);
        }
        protected override IEnumerable<Node> ParseScopeToken(Token token, TokenWalker walker)
        {
            if (token.Is(TokenType.Start | TokenType.Scope))
            {
                var before = walker.PeekProcedure(-2);
                var tokenType = TokenType.Scope;
                if (token.IsMatch("{"))
                {
                    if (before != null && !before.Is(TokenType.End | TokenType.Scope, TokenType.Structure, TokenType.TerminatorSymbol)) tokenType = TokenType.ObjectData;
                    else tokenType = TokenType.Scope;
                }
                else if (token.IsMatch("[")) tokenType = TokenType.ArrayData;

                Locality++;
                var children = new List<Node>();
                while (walker.Current != null && !walker.Current.Is(TokenType.End | TokenType.Scope))
                {
                    var n = BlockParseToken(walker);
                    if (n != null && !n.Is(NodeType.None)) children.Add(n);
                }
                walker.Walk();
                Locality--;
                yield return new Node(token.Clone(tokenType), NodeType.Block, children.ToArray());
            }
            else if (token.Is(TokenType.End | TokenType.Scope))
                yield return new Node(token, NodeType.None);
            else
                yield return new Node(token, NodeType.Compute, SequenceParseToken(walker));
        }
        protected override IEnumerable<Node> ParseSymbolToken(Token token, TokenWalker walker)
        {
            if (token.Is(TokenType.TerminatorSymbol, TokenType.Suffix))
                yield return new Node(token, NodeType.Plain);
            else if (token.Is(TokenType.DelimiterSymbol))
                yield return new Node(token, NodeType.Procedure, SequenceParseToken(walker));
            else if (token.Is(TokenType.Prefix) || token.Is(TokenType.ConcatenatorSymbol))
                yield return new Node(token, NodeType.Plain, CompactParseToken(walker));
            else if (token.IsMatch("=>", "?", "??") || token.Is(TokenType.Middle))
                yield return new Node(token, NodeType.Compute, CompactParseToken(walker));
            else if (token.IsMatch("="))
                yield return new Node(token, NodeType.Compute, (Compiler as Compiler).SetKeyword(walker.PeekProcedure(-2).Value, CompactParseToken(walker)));
            else
            {
                yield return new Node(token, NodeType.Compute);
                yield return CompactParseToken(walker);
            }
        }
        protected override IEnumerable<Node> ParseCommentToken(Token token, TokenWalker walker)
        {
            bool isblock = token.Value.Contains("\n") || token.Value.StartsWith("/*");
            yield return new Node(new Token(TokenType.Unknown), isblock ? NodeType.Procedure : NodeType.Block,
                new Node(token.Clone(Value: isblock ? token.Value : token.Value), NodeType.Helper),
                walker.Is(TokenType.End | TokenType.Scope) ? null : CompactParseToken(walker)
            );
        }
        protected override IEnumerable<Node> ParseStartToken(Token token, TokenWalker walker)
        {
            return ParseKeywordToken(token, walker);
        }
        protected override IEnumerable<Node> ParsePrefixToken(Token token, TokenWalker walker)
        {
            yield return new Node(token, NodeType.Plain, CompactParseToken(walker));
        }
        protected override IEnumerable<Node> ParseMiddleToken(Token token, TokenWalker walker)
        {
            yield return new Node(token, NodeType.Compute, CompactParseToken(walker));
        }
        protected override IEnumerable<Node> ParseSuffixToken(Token token, TokenWalker walker)
        {
            yield return new Node(token, NodeType.Plain);
        }
        protected override IEnumerable<Node> ParseEndToken(Token token, TokenWalker walker)
        {
            return ParseKeywordToken(token, walker);
        }
        protected override IEnumerable<Node> ParseUnknownToken(Token token, TokenWalker walker)
        {
            yield return new Node(token, NodeType.Unknown);
        }

    }
}
