// Converted from src/engine/DaRQ/DaRQ-Compiler/Parser.ts
using System;
using System.Collections.Generic;
using System.Linq;
using DaRQ.Compiler.Core;
using DaRQ.Compiler.Parser;

namespace DaRQ.DaRQ_Compiler
{
    public class Parser : DaRQ.JavaScriptCompiler.Parser
    {
        public dynamic DaRQCompiler { get; set; }

        public static Node CreateNode(string value = "", NodeType nodeType = NodeType.Plain, TokenType tokenType = TokenType.Unknown, params Node[] children)
        {
            return new Node(new Token(tokenType, value), nodeType, AccessType.Unknown, children);
        }

        public static Node CreatePlainNode(string value = "", params Node[] children)
        {
            return CreateNode(value, NodeType.Plain, TokenType.Unknown, children);
        }

        public static Node CreateNamespaceNode(string ns, Node node)
        {
            return CreateNode(ns + ".", NodeType.Plain, TokenType.ConcatenatorSymbol, node);
        }

        public static Node CreateCallFunctionNode(string name, params Node[] args)
        {
            var token = new Token(TokenType.FunctionKeyword, name);
            var n = new Node(token, NodeType.Call, AccessType.Unknown, args.ToList());
            return n; // trimming separators not implemented here
        }

        public static Node CreateDefineIdentifierNode(string name, Node value, string state = "var")
        {
            return new Node(new Token(TokenType.Statement, state), NodeType.Define, AccessType.Unknown, new List<Node> {
                CreateNode(name, NodeType.Call, TokenType.IdentifierKeyword),
                CreateNode("= "),
                value
            });
        }

        public static Node CreateDefineFunctionNode(string name, Node body, params Node[] args)
        {
            return new Node(new Token(TokenType.FunctionKeyword, name), NodeType.Define, AccessType.Unknown, new List<Node> {
                CreatePackNode(args),
                body
            });
        }

        public static Node CreatePackNode(params Node[] children)
        {
            return new Node(new Token(TokenType.Scope, "("), NodeType.Block, AccessType.Unknown, children.ToList());
        }

        public static Node CreateBlockNode(params Node[] children)
        {
            return new Node(new Token(TokenType.StartScope, "{"), NodeType.Block, AccessType.Unknown, children.ToList());
        }

        public static Node CreateProceduresNode(string value = "", params Node[] children)
        {
            return new Node(new Token(TokenType.Unknown, value), NodeType.Procedure, AccessType.Unknown, children.ToList());
        }

        public static Node CreateCallableNode(Node body, params Node[] args)
        {
            return CreatePackNode(new Node(new Token(TokenType.FunctionKeyword, ""), NodeType.Define, AccessType.Unknown, new List<Node> { CreatePackNode(args), body }));
        }

        protected Node ParseTokenLine(TokenWalker walker)
        {
            var node = ParseToken(walker);
            if (node.LastLeaf.Token.Is(TokenType.SeparatorSymbol))
            {
                node.LastLeaf.Token.Update(TokenType.SeparatorSymbol, ";");
            }
            else if (!node.Is(NodeType.Block))
            {
                return CreatePlainNode("", node, ParseTokenLine(walker));
            }
            return node;
        }

        protected Node ParseTokenBlock(TokenWalker walker)
        {
            var node = ParseToken(walker);
            if (!node.Is(NodeType.Block)) return CreateBlockNode(node);
            return node;
        }

        protected override Node ParseStatementToken(Token token, TokenWalker walker)
        {
            var next = walker.PeekProcedure();
            var next2 = walker.PeekProcedure(1);
            Func<Token, bool> notSeparator = t => !t.IsMatch(",") && !t.IsMatch(";");

            switch (token.Value.ToLower())
            {
                case "#":
                    var name = walker.Walk().Value;
                    if (next2?.IsMatch(":") == true) walker.Remove(next2.Location.Index, 1);
                    try { (this.Compiler as dynamic)?.SetCommand(name); } catch { }
                    return CreateDefineIdentifierNode(name, CreateProceduresNode("", CreateCallableNode(ParseToken(walker)), CreateNode($"{name}();", NodeType.Plain, TokenType.SeparatorSymbol)));
                case "if":
                    if (next?.IsMatch("(") == true) break;
                    return new Node(new Token(TokenType.Keyword, token.Value), NodeType.NormalSelector, AccessType.Unknown, new List<Node> {
                        ParseToken(walker).TrimSeparators(),
                        ParseTokenLine(walker)
                    });

                case "for":
                case "each":
                case "foreach":
                    token.Update(TokenType.Keyword, "for");
                    if (next?.IsMatch("each") == true)
                    {
                        walker.Walk();
                        if (next2?.IsMatch("(") == true) break;
                    }
                    else if (next?.IsMatch("(") == true) break;

                    if (walker.PeekProcedure(1)?.IsMatch("of", "in") == true || walker.PeekProcedure(2)?.IsMatch("of", "in") == true)
                        return new Node(token, NodeType.ComputationIterator | NodeType.CollectionIterator, AccessType.Unknown, new List<Node>
                        {
                            ParseToken(walker).TrimSeparators(),
                            ParseToken(walker)
                        });

                    return new Node(token, NodeType.ComputationIterator | NodeType.CollectionIterator, AccessType.Unknown, new List<Node>
                    {
                        new Node(null, NodeType.Plain, AccessType.Unknown, new List<Node> { ParseToken(walker) }),
                        ParseToken(walker)
                    });

                case "while":
                    if (next?.IsMatch("(") == true) break;
                    return new Node(token, NodeType.ConditionIterator, AccessType.Unknown, new List<Node>
                    {
                        new Node(null, NodeType.Plain, AccessType.Unknown, new List<Node> { ParseToken(walker).TrimSeparators() }),
                        ParseTokenLine(walker)
                    });

                case "try":
                    if (next?.IsMatch("to", "from", ",") == true) walker.Walk();
                    else if (next?.IsMatch("{") == true) return CreateProceduresNode(token.Value, ParseTokenBlock(walker));

                    var children = new List<Node>();
                    while (walker.Current != null && !walker.Current.IsMatch("catch", "finally"))
                    {
                        var n = ParseToken(walker);
                        if (!n.Is(NodeType.None)) children.Add(n);
                    }
                    return CreateProceduresNode(token.Value, CreateBlockNode(children.ToArray()));

                case "finally":
                    return CreateProceduresNode(token.Value, ParseTokenBlock(walker));

                case "catch":
                    return CreateProceduresNode(token.Value,
                        next?.Is(TokenType.Keyword) == true && next2?.Is(TokenType.SeparatorSymbol) == true
                            ? ((next.IsMatch("(") ? new[] { ParseToken(walker).TrimSeparators() } : new[] { CreatePackNode(ParseTokenSingle(walker).TrimSeparators()) }).Concat(new[] { ParseTokenBlock(walker.MoveToProcedure()) }).ToArray())
                            : new[] { ParseTokenBlock(walker) }
                    );

                case "return":
                case "yield":
                    return new Node(new Token(TokenType.Keyword, token.Value), NodeType.Procedure, AccessType.Unknown, new List<Node> { ParseTokenLine(walker) });

                default:
                    break;
            }
            return base.ParseStatementToken(token, walker);
        }

        protected override Node ParseStructureToken(Token token, TokenWalker walker)
        {
            var next = walker.PeekProcedure();
            var next2 = walker.PeekProcedure(1);
            switch (token.Value.ToLower())
            {
                case "command":
                    next = walker.MoveToProcedure().Walk();
                    try { (this.Compiler as dynamic)?.SetCommand(next.Value); } catch { }
                    return new Node(next.Clone(TokenType.FunctionKeyword, null, null), NodeType.Define, AccessType.Unknown, new List<Node> {
                        ParseToken(walker).TrimSeparators(), ParseToken(walker)
                    });
                case "function":
                    next = walker.MoveToProcedure().Walk();
                    try { (this.Compiler as dynamic)?.SetFunction(next.Value); } catch { }
                    return new Node(next.Clone(TokenType.FunctionKeyword, null, null), NodeType.Define, AccessType.Unknown, new List<Node> {
                        ParseToken(walker).TrimSeparators(), ParseToken(walker)
                    });

                case "do":
                case "doing":
                    if (token.IsMatch("do") && next?.IsMatch("{") == true) break;
                    if (next?.IsMatch(":") == true) walker.Walk();
                    var doChildren = new List<Node>();
                    while (walker.Current != null && !walker.Current.IsMatch("end"))
                    {
                        var n = ParseToken(walker);
                        if (!n.Is(NodeType.None)) doChildren.Add(n);
                    }
                    walker.Walk();
                    if (token.IsMatch("doing")) return CreateCallableNode(CreateBlockNode(doChildren.ToArray()));
                    else return CreateBlockNode(doChildren.ToArray());

                case "promise":
                case "then":
                case "otherwise":
                case "anyway":
                    if (next?.IsMatch("to", "from", ",") == true) walker.Walk();
                    var isInit = token.IsMatch("promise");
                    token.Update(TokenType.FunctionKeyword, isInit ? "new Promise" : token.IsMatch("otherwise") ? "catch" : token.IsMatch("anyway") ? "finally" : "then");
                    if (next == null) return new Node();
                    if (next.IsMatch("(") || (next.Is(TokenType.Keyword) && next2?.Is(TokenType.SeparatorSymbol) == true))
                        return new Node(token, NodeType.Call, AccessType.Unknown, new List<Node> { ParseToken(walker).TrimSeparators() });

                    var child = ParseToken(walker).TrimSeparators();
                    if (child.Is(NodeType.Block))
                    {
                        if (!isInit) child = new Node(new Token(TokenType.FunctionKeyword, ".then"), NodeType.Call, AccessType.Unknown, new List<Node> { CreateCallableNode(child.TrimSeparators()) });
                        else child = CreateCallableNode(child.TrimSeparators());
                    }
                    else if (!child.Is(NodeType.Call))
                    {
                        if (isInit) child = CreateCallableNode(child.TrimSeparators());
                        else child = CreateCallableNode(CreateBlockNode(CreateNode("WORKSPACE(data);"), child), CreateNode("data"));
                    }

                    return new Node(token, NodeType.Call, AccessType.Unknown, new List<Node> { child.TrimSeparators() });

                case "where":
                    token.Update(TokenType.FunctionKeyword, ".filter");
                    return new Node(token, NodeType.Call, AccessType.Unknown, new List<Node>
                    {
                        CreateCallableNode(ParseToken(walker).TrimSeparators(), CreateNode("data"))
                    });

                case "select":
                case "collect":
                    token.Update(TokenType.FunctionKeyword, token.Value.ToUpper());
                    return new Node(token, NodeType.Call, AccessType.Unknown, new List<Node> { ParseToken(walker).TrimSeparators() });

                case "distinct":
                    token.Update(TokenType.FunctionKeyword, ".filter");
                    return new Node(token, NodeType.Call, AccessType.Unknown, new List<Node>
                    {
                        CreateCallableNode(CreateProceduresNode("", CreateNode("self.indexOf(data) === index"), CreateNode("data")), CreateNode("data"))
                    });

                case "as":
                    var alias = walker.PeekProcedure();
                    if (alias != null)
                    {
                        var aliasValue = alias.Value;
                        walker.Walk();
                        var value = ParseToken(walker).TrimSeparators();
                        return CreateNode(aliasValue, NodeType.ObjectData, TokenType.ObjectData, value);
                    }
                    return CreateNode("", NodeType.ObjectData, TokenType.ObjectData);

                case "limit":
                    if (next?.IsMatch("by", "to") == true) walker.Walk();
                    token.Update(TokenType.FunctionKeyword, ".slice");
                    var nlimit = ParseToken(walker);
                    return new Node(token, NodeType.Call, AccessType.Unknown,
                        nlimit.Count > 1 ? nlimit.Children : new[] { CreateNode("0", NodeType.Plain, TokenType.NumberData), nlimit });

                case "order":
                    if (next?.IsMatch("by") == true) walker.Walk();
                    var norders = ParseToken(walker);
                    var orderItems = norders.Is(NodeType.Block) ? norders.Children : new List<Node> { norders };
                    var childrenOrder = new List<Node>();

                    string KeyAccess(Node key, string prefix)
                    {
                        if (key == null) return prefix;
                        if (key.Token != null && !string.IsNullOrEmpty(key.Token.Value) && key.Children.Count == 0)
                            return prefix + "." + key.Token.Value;
                        // fallback: join child token values by dot
                        var parts = new List<string>();
                        if (!string.IsNullOrEmpty(key.Token?.Value)) parts.Add(key.Token.Value);
                        parts.AddRange(key.Children.Select(c => c.Token?.Value ?? c.ToString()));
                        return prefix + "." + string.Join(".", parts.Where(p => !string.IsNullOrEmpty(p)));
                    }

                    foreach (var item in orderItems)
                    {
                        // build comparator string: (a,b)=>(a,b)=>a.key>b.key?1:a.key==b.key?0:-1
                        var keyExprA = KeyAccess(item, "a");
                        var keyExprB = KeyAccess(item, "b");
                        var comparator = $"(a,b)=>(a,b)=>{keyExprA}>{keyExprB}?1:{keyExprA}=={keyExprB}?0:-1";
                        var comparatorNode = CreateNode(comparator, NodeType.Plain, TokenType.Unknown);
                        childrenOrder.Add(CreateCallFunctionNode(".sort", comparatorNode));
                    }

                    return CreateNode("", NodeType.Procedure, TokenType.Unknown, childrenOrder.ToArray());

                case "reverse":
                case "desc":
                    token.Update(TokenType.FunctionKeyword, ".reverse");
                    return new Node(token, NodeType.Call, AccessType.Unknown);

                case "sort":
                case "asc":
                    token.Update(TokenType.FunctionKeyword, ".sort");
                    return new Node(token, NodeType.Call, AccessType.Unknown);

                case "join":
                case "concat":
                case "flat":
                case "fill":
                case "at":
                    return new Node(token.Update(TokenType.FunctionKeyword, "." + token.Value), NodeType.Call, AccessType.Unknown, new List<Node> { ParseToken(walker).TrimSeparators() });

                case "map":
                case "find":
                    return new Node(token.Update(TokenType.FunctionKeyword, "." + token.Value), NodeType.Call, AccessType.Unknown, new List<Node>
                    {
                        CreateCallableNode(ParseToken(walker).TrimSeparators(), CreateNode("data"))
                    });

                case "all":
                case "one":
                case "on":
                    token.Update(TokenType.FunctionKeyword, token.Value.ToUpper());
                    return new Node(token, NodeType.Call, AccessType.Unknown, new List<Node> { ParseToken(walker).TrimSeparators() });

                case "keys":
                case "values":
                    return CreateProceduresNode("", new Node(token.Update(TokenType.FunctionKeyword, "." + token.Value), NodeType.Call, AccessType.Unknown, new List<Node>()), CreateNode(".toArray()"));

                case "length":
                    return new Node(token.Update(TokenType.IdentifierKeyword, "." + token.Value), NodeType.Call, AccessType.Unknown);

                default:
                    break;
            }
            return base.ParseStructureToken(token, walker);
        }

        protected override Node ParseKeywordToken(Token token, TokenWalker walker)
        {
            var before = walker.PeekProcedure(-2);
            var next = walker.PeekProcedure();
            var next2 = walker.PeekProcedure(1);
            var node = base.ParseKeywordToken(token, walker);
            if (node != null && node.Is(NodeType.Call))
            {
                string callLable = null; string fname = null; string cname = null;
                try { callLable = (this.Compiler as dynamic)?.GetCallLable(node.Token.Value); } catch { }
                try { fname = (this.Compiler as dynamic)?.GetFunction(node.Token.Value); } catch { }
                try { cname = (this.Compiler as dynamic)?.GetCommand(node.Token.Value); } catch { }
                var fcname = fname ?? cname;
                if (node.Token.Is(TokenType.FunctionKeyword))
                {
                    node.Token.Update(TokenType.FunctionKeyword, fcname ?? node.Token.Value);
                    if (next?.Is(TokenType.Keyword) == true)
                        return new Node(token.Clone(TokenType.NamespaceKeyword, null, null), NodeType.Call, AccessType.Unknown, new List<Node> { ParseToken(walker) });
                    return node;
                }
                else if (node.Token.Is(TokenType.IdentifierKeyword))
                {
                    if (callLable != null) return CreateNode(callLable, NodeType.Procedure, TokenType.Data);
                    token = token.Clone(TokenType.IdentifierKeyword, fcname ?? node.Token.Value, null);
                    if (next?.Is(TokenType.Keyword, TokenType.Data, TokenType.StartScope) == true)
                    {
                        if (fcname != null || next.Is(TokenType.Data) || next.IsMatch("{", "(", "...") || next2?.IsMatch(",") == true)
                            return new Node(token.Clone(TokenType.FunctionKeyword, null, null), NodeType.Call, AccessType.Unknown, new List<Node> { next.Is(TokenType.SeparatorSymbol) ? ParseToken(walker) : ParseToken(walker).TrimSeparators() });
                        else if (next.Is(TokenType.Keyword)) return new Node(token.Clone(TokenType.NamespaceKeyword, null, null), NodeType.Call, AccessType.Unknown, new List<Node> { ParseToken(walker) });
                    }
                    else if (next?.Is(TokenType.Symbol) == true)
                    {
                        if (cname != null) return CreateNode($"{cname}()", NodeType.Procedure, TokenType.Data);
                        else if (next.Is(TokenType.SeparatorSymbol))
                        {
                            if (next2?.IsIndependent() == true)
                                return new Node(token, NodeType.Call, AccessType.Unknown);
                            else if (before?.IsIndependent() == true)
                                return new Node(token, NodeType.Call, AccessType.Unknown);
                        }
                        else return new Node(token.Clone(TokenType.IdentifierKeyword, null, null), NodeType.Call, AccessType.Unknown);
                    }
                }
            }
            return node;
        }

        protected override Node ParseSymbolToken(Token token, TokenWalker walker)
        {
            var next = walker.PeekProcedure();
            var next2 = walker.PeekProcedure(1);
            switch (token.Value.ToLower())
            {
                case "be":
                case "is":
                    if (next?.IsMatch("not") == true)
                    {
                        if (next2?.IsMatch("equal", "equals", "===") == true)
                        {
                            walker.MoveToProcedure(); walker.MoveToProcedure();
                            return ParseSymbolToken(token.Clone(TokenType.OperatorSymbol, "!==", null), walker);
                        }
                        else if (next2?.Is(TokenType.StringData) == true && next2?.IsMatch("", "empty") == true)
                        {
                            walker.MoveToProcedure(); walker.MoveToProcedure();
                            return ParseSymbolToken(token.Clone(TokenType.OperatorSymbol, "+ \"\" != \"\"", null), walker);
                        }
                        else if (next?.IsMatch("==", "=") == true)
                        {
                            walker.MoveToProcedure(); walker.MoveToProcedure();
                            return ParseSymbolToken(token.Clone(TokenType.OperatorSymbol, "!=", null), walker);
                        }
                        else { walker.MoveToProcedure(); return ParseSymbolToken(token.Clone(TokenType.OperatorSymbol, "!=", null), walker); }
                    }
                    else if (next?.IsMatch("equal", "equals", "===") == true)
                    {
                        walker.MoveToProcedure(); return ParseSymbolToken(token.Clone(TokenType.OperatorSymbol, "===", null), walker);
                    }
                    else if (next?.Is(TokenType.StringData) == true && next?.IsMatch("", "empty") == true)
                    {
                        walker.MoveToProcedure(); return ParseSymbolToken(token.Clone(TokenType.OperatorSymbol, "+ \"\" == \"\"", null), walker);
                    }
                    else return ParseSymbolToken(token.Clone(TokenType.OperatorSymbol, "==", null), walker);

                case "equal":
                case "equals":
                    if (next?.IsMatch("to", ",") == true)
                    {
                        walker.MoveToProcedure();
                        return ParseSymbolToken(token.Clone(TokenType.OperatorSymbol, "===", null), walker);
                    }
                    else return ParseSymbolToken(token.Clone(TokenType.OperatorSymbol, "===", null), walker);

                case "not":
                    if (next?.IsMatch("equal", "equals", "===") == true)
                    {
                        walker.MoveToProcedure();
                        return ParseSymbolToken(token.Clone(TokenType.OperatorSymbol, "!==", null), walker);
                    }
                    else if (next?.IsMatch("be", "==", "=") == true)
                    {
                        walker.MoveToProcedure();
                        return ParseSymbolToken(token.Clone(TokenType.OperatorSymbol, "!=", null), walker);
                    }
                    else if (next?.Is(TokenType.StringData) == true && next?.IsMatch("", "empty") == true)
                    {
                        walker.MoveToProcedure();
                        return ParseSymbolToken(token.Clone(TokenType.OperatorSymbol, "+ \"\" != \"\"", null), walker);
                    }
                    else return ParseSymbolToken(token.Clone(TokenType.OperatorSymbol, "!", null), walker);
                default:
                    break;
            }
            if (next?.Is(TokenType.Symbol) == true) return ParseSymbolToken(token.Clone(token.Type | next.Type, token.Value + next.Value, null), walker.MoveToProcedure());
            else if (token.Is(TokenType.SeparatorSymbol) && (next?.IsDependent() == true || next?.IsIndependent() == true))
                return new Node { Token = token, Type = NodeType.Plain };
            return base.ParseSymbolToken(token, walker);
        }
    }
}
