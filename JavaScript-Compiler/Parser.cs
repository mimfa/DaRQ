// Converted from src/engine/DaRQ/JavaScript-Compiler/Parser.ts
using System;
using System.Collections.Generic;
using System.Linq;
using DaRQ.Compiler;
using DaRQ.Compiler.Core;
using DaRQ.Compiler.Parser;

namespace DaRQ.JavaScriptCompiler
{
    public class Parser : DaRQ.Compiler.Parser.Parser
    {
        protected override Node ParseTokenSingle(TokenWalker walker)
        {
            return base.ParseToken(walker);
        }

        protected override Node ParseToken(TokenWalker walker)
        {
            var tokens = ParseTokens(walker).ToList();
            if (tokens.Count > 1) return new Node(null, NodeType.Plain, AccessType.Unknown, tokens);
            if (tokens.Count == 1) return tokens[0];
            return new Node();
        }

        protected override IEnumerable<Node> ParseTokens(TokenWalker walker)
        {
            yield return base.ParseToken(walker);
            if (walker.Current != null)
            {
                if (walker.Current.IsMatch(";"))
                    yield return base.ParseToken(walker);
                else if (walker.Current.IsDependent() && !walker.Current.IsIndependent())
                    foreach (var node in ParseTokens(walker)) yield return node;
            }
        }

        protected override Node ParseStatementToken(Token token, TokenWalker walker)
        {
            switch (token.Value)
            {
                case "if":
                    return new Node(token, NodeType.NormalSelector, AccessType.Unknown, new List<Node> {
                        ParseToken(walker.MoveToProcedure()),
                        ParseToken(walker.MoveToProcedure()),
                        (walker.PeekProcedure()?.IsMatch("else") == true) ? ParseToken(walker) : null
                    }.Where(n => n != null));
                case "else":
                    return ParseToken(walker);

                case "switch":
                    return new Node(token, NodeType.LongSelector, AccessType.Unknown, new List<Node> {
                        ParseToken(walker.MoveToProcedure()),
                        ParseToken(walker.MoveToProcedure())
                    });
                case "case":
                    var caseChildren = new List<Node> { ParseToken(walker) };
                    caseChildren.AddRange(walker.MoveToProcedure().MapUntil(() => walker.Current == null || walker.Current.IsMatch("case", "default") || walker.Current.Is(TokenType.EndScope), () => ParseToken(walker)).ToList());
                    return new Node(token, NodeType.Procedure, AccessType.Unknown, caseChildren);
                case "default":
                    var defaultChildren = walker.MoveToProcedure().MapUntil(() => walker.Current == null || walker.Current.IsMatch("case", "default") || walker.Current.Is(TokenType.EndScope), () => ParseToken(walker)).ToList();
                    return new Node(token, NodeType.Procedure, AccessType.Unknown, defaultChildren);

                case "for":
                    return new Node(token, NodeType.ComputationIterator | NodeType.CollectionIterator, AccessType.Unknown, new List<Node> {
                        ParseToken(walker), ParseToken(walker)
                    });

                case "while":
                    return new Node(token, NodeType.ConditionIterator, AccessType.Unknown, new List<Node> {
                        ParseToken(walker), ParseToken(walker)
                    });

                case "do":
                    return new Node(token, NodeType.PostConditionIterator, AccessType.Unknown, new List<Node> {
                        ParseToken(walker), ParseToken(walker.MoveToProcedure())
                    });

                case "break":
                case "continue":
                    return new Node(token, NodeType.Plain, AccessType.Unknown);

                case "try":
                case "finally":
                    return new Node(token, NodeType.Plain, AccessType.Unknown, new List<Node> { ParseToken(walker) });

                case "catch":
                    var catchChildren = new List<Node>();
                    if (walker.PeekProcedure()?.IsMatch("(") == true) catchChildren.Add(ParseToken(walker));
                    catchChildren.Add(ParseToken(walker));
                    return new Node(token, NodeType.Plain, AccessType.Unknown, catchChildren);

                case "return":
                case "yield":
                    return new Node(token, NodeType.Procedure, AccessType.Unknown, new List<Node> { ParseToken(walker) });

                case "throw":
                    return new Node(token, NodeType.Procedure, AccessType.Unknown, new List<Node> { ParseToken(walker) });

                case "import":
                case "export":
                    // support import { x as y } from '...'; and export default
                    if (walker.PeekProcedure()?.IsMatch("{") == true || walker.PeekProcedure()?.IsMatch("*") == true)
                        return new Node(token, NodeType.Procedure, AccessType.Unknown, new List<Node> { ParseToken(walker) });
                    return new Node(token, NodeType.Procedure, AccessType.Unknown, new List<Node> { ParseToken(walker) });

                case "void":
                case "const":
                case "let":
                case "var":
                    return new Node(token, NodeType.Define, AccessType.Unknown, new List<Node> { ParseToken(walker) });

                case "with":
                case "debugger":
                    return new Node(token, NodeType.Procedure, AccessType.Unknown, new List<Node> { ParseToken(walker) });

                case "class":
                    var nameNode = walker.PeekProcedure();
                    var classChildren = new List<Node>();
                    if (nameNode != null && nameNode.Is(TokenType.Identifier))
                        classChildren.Add(ParseToken(walker));
                    if (walker.PeekProcedure()?.IsMatch("extends") == true) classChildren.Add(ParseToken(walker));
                    if (walker.PeekProcedure()?.Is(TokenType.StartScope) == true) classChildren.Add(ParseToken(walker));
                    return new Node(token, NodeType.Define, AccessType.Unknown, classChildren);

                case "async":
                    var next = walker.PeekProcedure();
                    if (next != null && next.IsMatch("function"))
                        return new Node(token, NodeType.Define, AccessType.Unknown, new List<Node> { ParseToken(walker) });
                    return new Node(token, NodeType.Procedure, AccessType.Unknown, new List<Node> { ParseToken(walker) });

                case "await":
                    return new Node(token, NodeType.Compute, AccessType.Unknown, new List<Node> { ParseToken(walker) });
            }
            return null;
        }

        protected override Node ParseAccessToken(Token token, TokenWalker walker)
        {
            switch (token.Value)
            {
                case "private":
                    var n1 = ParseToken(walker);
                    n1.AccessType = AccessType.Private;
                    return new Node(token, NodeType.Define, AccessType.Unknown, new List<Node> { n1 });
                case "protected":
                    var n2 = ParseToken(walker);
                    n2.AccessType = AccessType.Protected;
                    return new Node(token, NodeType.Define, AccessType.Unknown, new List<Node> { n2 });
                case "internal":
                    var n3 = ParseToken(walker);
                    n3.AccessType = AccessType.Internal;
                    return new Node(token, NodeType.Define, AccessType.Unknown, new List<Node> { n3 });
                case "public":
                    var n4 = ParseToken(walker);
                    n4.AccessType = AccessType.Public;
                    return new Node(token, NodeType.Define, AccessType.Unknown, new List<Node> { n4 });
                default:
                    return new Node(token, NodeType.Define, AccessType.Unknown, new List<Node> { ParseToken(walker) });
            }
        }

        protected override Node ParseStructureToken(Token token, TokenWalker walker)
        {
            switch (token.Value)
            {
                case "of":
                case "in":
                    return new Node(token, NodeType.Compute, AccessType.Unknown, new List<Node> { ParseToken(walker).TrimSeparators() });
                case "get":
                case "set":
                case "function":
                    var t = walker.PeekProcedure();
                    if (t != null && t.Is(TokenType.Keyword))
                    {
                        return new Node(t.Clone(TokenType.FunctionKeyword, null, null), NodeType.Define, AccessType.Unknown, new List<Node> {
                            ParseToken(walker).TrimSeparators(), ParseToken(walker)
                        });
                    }
                    else
                    {
                        return new Node(new Token(TokenType.FunctionKeyword, ""), NodeType.Define, AccessType.Unknown, new List<Node> {
                            ParseToken(walker).TrimSeparators(), ParseToken(walker)
                        });
                    }
                default:
                    return new Node(token, NodeType.Define, AccessType.Unknown, new List<Node> { ParseToken(walker), ParseToken(walker) });
            }
        }

        protected override Node ParseDataToken(Token token, TokenWalker walker)
        {
            if (token.Is(TokenType.PathData))
            {
                if (token.Is(TokenType.RegExPathData)) return new Node(token, NodeType.Plain, AccessType.Unknown);
                else return new Node(token.Clone(null, System.Text.Json.JsonSerializer.Serialize(token.Value), null), NodeType.Plain, AccessType.Unknown);
            }
            else if (token.Is(TokenType.StringData))
            {
                if (token.Is(TokenType.TemplateStringData)) return new Node(token.Clone(null, "`" + token.Value + "`", null), NodeType.Plain, AccessType.Unknown);
                else return new Node(token.Clone(null, System.Text.Json.JsonSerializer.Serialize(token.Value), null), NodeType.Plain, AccessType.Unknown);
            }

            if (token.Is(TokenType.ArrayData)) return new Node(token.Clone(null, "[]", null), NodeType.Block, AccessType.Unknown);
            if (token.Is(TokenType.ObjectData)) return new Node(token.Clone(null, "{}", null), NodeType.Block, AccessType.Unknown);

            return new Node(token.Clone(null, token.Value.ToLower(), null), NodeType.Plain, AccessType.Unknown);
        }

        protected override Node ParseKeywordToken(Token token, TokenWalker walker)
        {
            var next = walker.PeekProcedure();
            if (next != null && next.Is(TokenType.StartScope))
            {
                if (next.IsMatch("("))
                {
                    return new Node(token.Clone(TokenType.FunctionKeyword, null, null), NodeType.Call, AccessType.Unknown, base.ParseToken(walker).Children);
                }
            }
            // handle 'new' and 'await'
            if (token.Value == "new")
            {
                return new Node(token, NodeType.Call, AccessType.Unknown, new List<Node> { ParseToken(walker) });
            }
            if (token.Value == "await")
            {
                return new Node(token, NodeType.Compute, AccessType.Unknown, new List<Node> { ParseToken(walker) });
            }

            if (next != null && next.Is(TokenType.Symbol))
            {
                if (next.Is(TokenType.ConcatenatorSymbol))
                {
                    return new Node(next.IsMatch(".") ? token.Clone(TokenType.NamespaceKeyword, null, null) : token, NodeType.Plain, AccessType.Unknown, new List<Node> { ParseToken(walker) });
                }
                else if (next.Is(TokenType.OperatorSymbol))
                    return new Node(token.Clone(TokenType.IdentifierKeyword, null, null), NodeType.Plain, AccessType.Unknown, new List<Node> { ParseToken(walker) });
            }
            return new Node(token.Clone(TokenType.IdentifierKeyword, null, null), NodeType.Call, AccessType.Unknown);
        }

        protected override Node ParseSymbolToken(Token token, TokenWalker walker)
        {
            if (token.IsMatch(";")) return new Node(token, NodeType.Plain, AccessType.Unknown);
            if (token.IsMatch(",")) return new Node(token, NodeType.Procedure, AccessType.Unknown, new List<Node> { ParseToken(walker) });
            // arrow function
            if (token.IsMatch("=>")) return new Node(token, NodeType.Define, AccessType.Unknown, new List<Node> { ParseToken(walker) });
            // spread
            if (token.IsMatch("...")) return new Node(token, NodeType.Compute, AccessType.Unknown, new List<Node> { ParseToken(walker) });
            // optional chaining
            if (token.IsMatch("?.") || token.IsMatch("?")) return new Node(token, NodeType.Compute, AccessType.Unknown, new List<Node> { ParseToken(walker) });
            // nullish coalescing
            if (token.IsMatch("??")) return new Node(token, NodeType.Compute, AccessType.Unknown, new List<Node> { ParseToken(walker) });
            if (token.Is(TokenType.OperatorSymbol)) return new Node(token, NodeType.Compute, AccessType.Unknown, new List<Node> { ParseToken(walker) });
            if (token.Is(TokenType.ConcatenatorSymbol)) return new Node(token, token.IsMatch(".") ? NodeType.Plain : NodeType.Compute, AccessType.Unknown, new List<Node> { ParseToken(walker) });
            return new Node(token, NodeType.Compute, AccessType.Unknown, new List<Node> { ParseToken(walker) });
        }

        protected override Node ParseScopeToken(Token token, TokenWalker walker)
        {
            if (token.Is(TokenType.StartScope))
            {
                var before = walker.PeekProcedure(-2);
                var tokenType = TokenType.Scope;
                var nodeType = NodeType.Block;
                if (token.IsMatch("{"))
                {
                    if (!(before != null && (before.Is(TokenType.EndScope) || before.IsMatch(";")))) tokenType = TokenType.ObjectData;
                    else tokenType = TokenType.Scope;
                }
                else if (token.IsMatch("[")) tokenType = TokenType.ArrayData;

                var children = new List<Node>();
                while (walker.Current != null && !walker.Current.Is(TokenType.EndScope))
                {
                    var n = ParseToken(walker);
                    if (n != null && !n.Is(NodeType.None)) children.Add(n);
                }
                walker.Walk();
                return new Node(token.Clone(tokenType, null, null), nodeType, AccessType.Unknown, children);
            }
            else if (token.Is(TokenType.EndScope))
            {
                return new Node(new Token(TokenType.None, ""), NodeType.None, AccessType.Unknown);
            }
            else
            {
                return new Node(token, NodeType.Compute, AccessType.Unknown, new List<Node> { ParseToken(walker) });
            }
        }

        protected override Node ParseFacilitatorToken(Token token, TokenWalker walker)
        {
            return new Node(token, NodeType.Compute, AccessType.Unknown, new List<Node> { ParseToken(walker) });
        }

        protected override Node ParseCommentToken(Token token, TokenWalker walker)
        {
            var helper = new Node(new Token(TokenType.Unknown), NodeType.Helper, AccessType.Unknown, new List<Node> {
                new Node(token.Clone(null, token.Value.Contains("\n") ? ("/*" + token.Value.Replace("*/","*\\/") + "*/") : ("//" + token.Value + "\n")), NodeType.Helper, AccessType.Unknown)
            });
            var children = new List<Node> { helper };
            if (!(walker.Is(TokenType.EndScope))) children.Add(ParseToken(walker));
            return new Node(new Token(TokenType.Unknown), NodeType.Program, AccessType.Unknown, children);
        }

        protected override Node ParseUnknownToken(Token token, TokenWalker walker)
        {
            return new Node(token, NodeType.Unknown, AccessType.Unknown);
        }
    }
}
