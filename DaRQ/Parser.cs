using System;
using System.Collections.Generic;
using System.Linq;
using MiMFa.Compiler.Model;
using MiMFa.Compiler.Walker;

namespace MiMFa.Compiler.DaRQ
{
    public class Parser : JavaScript.Parser
    {
        public new Compiler Compiler { get; set; }

        public string[] EqualsSign = new string[] { "=", "=>", "->" };


        public override bool Initialize(MiMFa.Compiler.Compiler compiler)
        {
            if (compiler != null) base.Initialize(Compiler = compiler == null ? Compiler : compiler as Compiler);
            return true;
        }

        public int TargetLocation { get; set; } = 0;

        public override IEnumerable<Node> Parse(TokenWalker walker, MiMFa.Compiler.Compiler compiler = null)
        {
            if (!Initialize(compiler)) yield break;
            TargetLocation = -1;
            Locality = 0;
            while (!walker.IsEnded)
                yield return ParseTokenSequence(walker);
        }

        protected override Node ParseToken(TokenWalker walker)
        {
            if (TargetLocation >= 0 && Locality > TargetLocation++)
                return new Node();
            else TargetLocation = -1;
            return base.ParseToken(walker);
        }

        protected virtual Node ParseTokenSequence(TokenWalker walker)
        {
            Locality++;
            var nodes = ParseTokenSequences(walker).ToList();
            Locality--;
            if (nodes.Count > 1) return new Node(null, NodeType.Plain, nodes.ToArray());
            if (nodes.Count == 1) return nodes[0];
            return new Node();
        }
        protected virtual IEnumerable<Node> ParseTokenSequences(TokenWalker walker)
        {
            yield return base.ParseToken(walker);
            if (walker.Current != null)
            {
                var next = walker.PeekProcedure();
                if (next != null && next.Is(TokenType.Structure | TokenType.Keyword))
                    foreach (var node in ParseTokenSequences(walker))
                        yield return node;
            }
        }

        protected virtual Node SeparatedParseToken(TokenWalker walker)
        {
            Locality++;
            var tokens = SeparatedParseTokens(walker).ToList();
            Locality--;
            if (tokens.Count > 1) return new Node(null, NodeType.Plain, tokens.ToArray());
            if (tokens.Count == 1) return tokens[0];
            return new Node();
        }
        protected virtual IEnumerable<Node> SeparatedParseTokens(TokenWalker walker)
        {
            foreach (var node in BasicParseTokens(walker)) yield return node;
            if (walker.Current != null)
            {
                var next = walker.PeekProcedure();
                if (next != null && (next.Is(TokenType.Structure | TokenType.Keyword) || next.Is(TokenType.OperatorSymbol, TokenType.Facilitator) || next.IsMatch("[")))
                    foreach (var node in SeparatedParseTokens(walker)) yield return node;
                yield break;
            }
        }

        protected virtual Node ParseTokenLine(TokenWalker walker)
        {
            return CreateLineNode(ParseTokenSequence(walker));
        }
        protected virtual Node ParseTokenBlock(TokenWalker walker)
        {
            var node = ParseTokenSequence(walker);
            if (!node.Is(NodeType.Block)) return CreateBlockNode(node);
            return node;
        }


        protected override IEnumerable<Node> ParseStatementToken(Token token, TokenWalker walker)
        {
            var before = walker.PeekProcedure(-2);
            var next = walker.PeekProcedure();
            var next2 = walker.PeekProcedure(1);

            switch (token.Value.ToLower())
            {
                case "#":
                    var name = walker.Walk().Value;
                    if (next2?.Is(TokenType.SeparatorSymbol, TokenType.TerminatorSymbol) == true || next2?.IsMatch(EqualsSign) == true) walker.Remove(next2);
                    try { Compiler?.SetActionCommand(name); } catch { }
                    yield return CreateLineNode(
                        CreateCallNode(
                            CreatePackNode(
                                CreateDefineIdentifierNode(name, 
                                    CreateCallableNode(
                                        ParseTokenSequence(walker)
                                    ), null
                                )
                            )
                       )
                    );
                    yield break;
                case "command":
                    if (next?.IsMatch("(") == true)
                    {
                        token.Value = "function";
                        break;
                    }
                    var cname = walker.Walk();
                    next = walker.PeekProcedure();
                    if (next?.IsMatch("(") == true)
                    {
                        Compiler?.SetFunctionCommand(cname.Value);
                        yield return new Node(cname.Clone(TokenType.FunctionKeyword, null, null), NodeType.Define,
                            ParseTokenSequence(walker).TrimSeparators(),
                            ParseTokenBlock(walker)
                        );
                    }
                    else
                    {
                        Compiler?.SetDirectionCommand(cname.Value);
                        if (next.Is(TokenType.OperatorSymbol))
                            walker.Walk();
                        yield return CreateNode("var", NodeType.Define, TokenType.Statement,
                            new Node(cname.Clone(TokenType.IdentifierKeyword, null, null), NodeType.Call),
                            CreateNode("="),
                            ParseTokenLine(walker)
                        );
                    }
                    yield break;

                case "if":
                    if (next?.IsMatch("(") == true) break;
                    var cond = SeparatedParseToken(walker).TrimSeparators();
                    var onTrue = ParseTokenLine(FitWalker(walker));
                    var onFalse = (walker.PeekProcedure()?.IsMatch("else") == true) ? ParseTokenLine(walker.MoveToProcedure()) : null;
                    bool isnormal = before == null ||
                        onTrue.Token.IsMatch("{") ||
                        onFalse.Token.IsMatch("{") ||
                        before.IsMatch("{", "do", "end", "else") ||
                        before.Is(TokenType.TerminatorSymbol, TokenType.EndScope);
                    yield return new Node(
                        token,
                        isnormal ? NodeType.NormalSelector : NodeType.ShortSelector,
                        cond, isnormal ? CreateLineNode(onTrue) : onTrue.TrimSeparators(), isnormal ? CreateLineNode(onFalse) : onFalse.TrimSeparators()
                    );
                    yield break;

                case "for":
                case "each":
                case "foreach":
                    token.Value = "for";
                    if (next?.IsMatch("each") == true)
                    {
                        walker.Remove(next);
                        if (next2?.IsMatch("(") == true) break;
                    }
                    else if (next?.IsMatch("(") == true) break;

                    if (walker.PeekProcedure(1)?.IsMatch("of", "in") == true || walker.PeekProcedure(2)?.IsMatch("of", "in") == true)
                        yield return new Node(token, NodeType.CollectionIterator,
                            SeparatedParseToken(walker).TrimSeparators(),
                            ParseTokenLine(FitWalker(walker))
                        );
                    else yield return new Node(token, NodeType.ComputationIterator,
                        new Node(null, NodeType.Plain,
                        CreateNode("", SeparatedParseToken(walker).TrimSeparators(), CreateNode(";", TokenType.TerminatorSymbol)),
                        CreateNode("", SeparatedParseToken(FitWalker(walker)).TrimSeparators(), CreateNode(";", TokenType.TerminatorSymbol)),
                        SeparatedParseToken(FitWalker(walker)).TrimSeparators()),
                        ParseTokenLine(FitWalker(walker))
                    );
                    yield break;

                case "while":
                    if (next?.IsMatch("(") == true) break;
                    yield return new Node(token, NodeType.ConditionIterator, 
                        new Node(null, NodeType.Plain, ParseTokenSequence(walker).TrimSeparators()),
                        ParseTokenLine(walker)
                    );
                    yield break;

                case "try":
                    if (next?.IsMatch("to", "from") == true || next?.Is(TokenType.SeparatorSymbol) == true) walker.Walk();
                    else if (next?.IsMatch("{") == true) yield return CreateProceduresNode(token.Value, ParseTokenBlock(walker));
                    else
                    {
                        var children = new List<Node>();
                        while (walker.Current != null && !walker.Current.IsMatch("catch", "finally"))
                        {
                            var n = ParseTokenLine(walker);
                            if (!n.Is(NodeType.None)) children.Add(n);
                        }
                        yield return CreateProceduresNode(token.Value, CreateBlockNode(children.ToArray()));
                    }
                    yield break;

                case "finally":
                    yield return CreateProceduresNode(token.Value, ParseTokenBlock(walker));
                    break;

                case "catch":
                    yield return CreateProceduresNode(token.Value,
                        next?.Is(TokenType.Keyword) == true && next2?.Is(TokenType.SeparatorSymbol) == true
                            ? (next.IsMatch("(") ? new[] { ParseTokenSequence(walker).TrimSeparators() } : new[] { CreatePackNode(ParseTokenSingle(walker).TrimSeparators()) }).Concat(new[] { ParseTokenBlock(walker.MoveToProcedure()) }).ToArray()
                            : new[] { ParseTokenBlock(walker) }
                    );
                    yield break;

                case "return":
                case "yield":
                    yield return new Node(new Token(TokenType.Keyword, token.Value), NodeType.Procedure, ParseTokenLine(walker));
                    yield break;
            }
            foreach(var node in base.ParseStatementToken(token, walker))
                yield return node;
        }
        protected override IEnumerable<Node> ParseStructureToken(Token token, TokenWalker walker)
        {
            var before = walker.PeekProcedure(-2);
            var next = walker.PeekProcedure();
            var next2 = walker.PeekProcedure(1);
            string dot = before == null || before.Value != "." ? "." : null;
            switch (token.Value.ToLower())
            {
                case "function":
                    if (next?.IsMatch("(") == true) break;
                    next = walker.Walk();
                    Compiler?.SetFunction(next.Value);
                    yield return new Node(next.Clone(TokenType.FunctionKeyword, null, null), NodeType.Define,
                        ParseTokenSequence(walker).TrimSeparators(),
                        ParseTokenBlock(walker)
                    );
                    yield break;

                case "do":
                case "doing":
                    if (token.IsMatch("do") && next?.IsMatch("{") == true) break;
                    if (next?.Is(TokenType.SeparatorSymbol) == true) walker.Walk();
                    Locality++;
                    var doChildren = new List<Node>();
                    while (walker.Current != null && !walker.Current.IsMatch("end"))
                    {
                        var n = ParseTokenLine(walker);
                        if (!n.Is(NodeType.None)) doChildren.Add(n);
                    }
                    walker.Walk();
                    Locality--;
                    if (walker.Is(TokenType.TerminatorSymbol, TokenType.SeparatorSymbol))
                        walker.Walk();
                    if (token.IsMatch("doing"))
                        yield return CreateCallableNode(CreateBlockNode(doChildren.ToArray()));
                    else yield return CreateBlockNode(doChildren.ToArray());
                    yield break;

                case "promise":
                case "then":
                case "otherwise":
                case "anyway":
                    if (next?.IsMatch("to", "from") == true || next?.Is(TokenType.SeparatorSymbol) == true) walker.Walk();
                    var isInit = token.IsMatch("promise");
                    token.Update(TokenType.FunctionKeyword, isInit ? "new Promise" : token.IsMatch("otherwise") ? $"{dot}catch" : token.IsMatch("anyway") ? $"{dot}finally" : $"{dot}then");
                    if (next == null) yield return new Node();
                    else if (next.IsMatch("(") || (next.Is(TokenType.Keyword) && next2?.Is(TokenType.SeparatorSymbol, TokenType.TerminatorSymbol) == true))
                        yield return new Node(token, NodeType.Call, ParseToken(walker).TrimSeparators());

                    var child = ParseToken(walker);
                    if (child != null)
                    {
                        if (child.Is(NodeType.Block))
                        {
                            if (isInit) child = CreateCallableNode(child);
                            else child = CreateCallableNode(child.Insert(0, CreateNode("WORKSPACE(data);")), CreateNode("data", NodeType.Plain, TokenType.Keyword));
                        }
                        else if (!child.Is(NodeType.Call) || child.Count > 0)
                        {
                            if (isInit) child = CreateCallableNode(child);
                            else child = CreateCallableNode(CreateBlockNode(CreateNode("WORKSPACE(data);"), CreateLineNode(child)), CreateNode("data", NodeType.Plain, TokenType.Keyword));
                        }
                        else child = child.TrimSeparators();
                    }
                    yield return new Node(token, NodeType.Call, child);
                    yield break;

                case "where":
                    token.Update(TokenType.FunctionKeyword, "filter");
                    yield return new Node(token, NodeType.Call, 
                        CreateCallableNode(ParseTokenSequence(walker).TrimSeparators(), CreateNode("data"))
                    );
                    yield break;

                case "select":
                case "collect":
                    token.Update(TokenType.FunctionKeyword, token.Value.ToUpper());
                    yield return new Node(token, NodeType.Call, ParseToken(walker).TrimSeparators());
                    yield break;

                case "distinct":
                    token.Update(TokenType.FunctionKeyword, "filter");
                    yield return new Node(token, NodeType.Call,
                        CreateCallableNode(CreateProceduresNode("", CreateNode("self.indexOf(data) === index"), CreateNode("data")), CreateNode("data"))
                    );
                    yield break;

                case "as":
                    var alias = walker.PeekProcedure();
                    if (alias != null)
                    {
                        var aliasValue = alias.Value;
                        walker.Walk();
                        var value = ParseToken(walker).TrimSeparators();
                        yield return CreateNode(aliasValue, NodeType.Compute, TokenType.ObjectData, value);
                    }
                    yield return CreateNode("", NodeType.Compute, TokenType.ObjectData);
                    yield break;

                case "limit":
                    if (next?.IsMatch("by", "to") == true) walker.Walk();
                    token.Update(TokenType.FunctionKeyword, "slice");
                    var nlimit = ParseToken(walker);
                    yield return new Node(token, NodeType.Call,
                        nlimit.Count > 1 ? nlimit.Children.ToArray() : new[] { CreateNode("0", NodeType.Plain, TokenType.NumberData), nlimit });
                    yield break;

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
                        childrenOrder.Add(CreateCallFunctionNode("sort", comparatorNode));
                    }

                    yield return CreateNode("", NodeType.Procedure, TokenType.Unknown, childrenOrder.ToArray());
                    yield break;

                case "reverse":
                case "desc":
                    token.Update(TokenType.FunctionKeyword, "reverse");
                    yield return new Node(token, NodeType.Call);
                    yield break;

                case "sort":
                case "asc":
                    token.Update(TokenType.FunctionKeyword, "sort");
                    yield return new Node(token, NodeType.Call);
                    yield break;

                case "join":
                case "concat":
                case "flat":
                case "fill":
                case "at":
                    yield return new Node(token.Update(TokenType.FunctionKeyword, dot + token.Value), NodeType.Call, ParseToken(walker).TrimSeparators() );
                    yield break;

                case "map":
                case "find":
                    yield return new Node(token.Update(TokenType.FunctionKeyword, dot + token.Value), NodeType.Call, 
                        CreateCallableNode(ParseToken(walker).TrimSeparators(), CreateNode("data"))
                    );
                    yield break;

                case "keys":
                case "values":
                    yield return CreateNode("", new Node(token.Update(TokenType.FunctionKeyword, dot + token.Value), NodeType.Call), CreateCallFunctionNode(".toArray"));
                    yield break;

                case "length":
                    yield return new Node(token.Update(TokenType.IdentifierKeyword, dot + token.Value), NodeType.Call);
                    yield break;

                default:
                    break;
            }
            foreach (var node in base.ParseStructureToken(token, walker))
                yield return node;
        }
        protected override IEnumerable<Node> ParseKeywordToken(Token token, TokenWalker walker)
        {
            var before = walker.PeekProcedure(-2);
            var next = walker.PeekProcedure();
            var next2 = walker.PeekProcedure(1);
            var nodes = base.ParseKeywordToken(token, walker).ToArray();

            if (nodes.Length == 1 && nodes[0].Is(NodeType.Call))
            {
                Node node = nodes[0];
                string fname = Compiler?.GetFunction(node.Token.Value);
                string fcname = Compiler?.GetFunctionCommand(node.Token.Value);
                string dcname = Compiler?.GetDirectionCommand(node.Token.Value);
                string acname = Compiler?.GetActionCommand(node.Token.Value);
                var ffc = fname ?? fcname;
                if (node.Token.Is(TokenType.FunctionKeyword))
                {
                    if (before?.Is(TokenType.ConcatenatorSymbol) == true) yield return node;
                    else
                    {
                        node.Token.Update(TokenType.FunctionKeyword, acname ?? ffc ?? node.Token.Value);
                        foreach (var n in nodes)
                            yield return n;
                        if (!string.IsNullOrEmpty(acname) &&
                                next != null &&
                                next.Is(TokenType.Keyword)
                            )
                            yield return new Node(token.Clone(TokenType.NamespaceKeyword), NodeType.Call, CreateNode(next.Is(TokenType.Structure)?"":".", NodeType.Plain, TokenType.ConcatenatorSymbol), ParseToken(walker));
                    }
                    yield break;
                }
                else if (node.Token.Is(TokenType.IdentifierKeyword))
                {
                    if (acname != null) yield return CreateNode(acname, NodeType.Procedure, TokenType.Data);
                    else
                    {
                        token.Update(TokenType.IdentifierKeyword, ffc ?? dcname ?? node.Token.Value);
                        if (next?.Is(TokenType.Keyword, TokenType.Data, TokenType.StartScope) == true)
                        {
                            if ((dcname == null || ffc != null) && !next.Is(TokenType.Structure) && (ffc != null || next.Is(TokenType.Data) || next.IsMatch("{", "(", "...") || next2?.Is(TokenType.SeparatorSymbol, TokenType.TerminatorSymbol) == true))
                            {
                                yield return new Node(
                                    token.Clone(TokenType.FunctionKeyword),
                                    NodeType.Call,
                                    ParseToken(walker).TrimSeparators()
                                );
                                yield break;
                            }
                            else if (next.Is(TokenType.Keyword))
                            {
                                yield return new Node(token.Clone(TokenType.NamespaceKeyword), NodeType.Call, CreateNode(next.Is(TokenType.Structure)?"":".", NodeType.Plain, TokenType.ConcatenatorSymbol), ParseToken(walker));
                                yield break;
                            }
                        }
                        else if (next?.Is(TokenType.Symbol, TokenType.EndScope) == true)
                        {
                            if (fcname != null)
                            {
                                yield return CreateCallFunctionNode(fcname);
                                yield break;
                            }
                            else if (next.Is(TokenType.SeparatorSymbol, TokenType.TerminatorSymbol))
                            {
                                if (next2?.IsIndependent() == true)
                                {
                                    yield return new Node(token, NodeType.Call);
                                    yield break;
                                }
                                else if (before != null && (before.IsIndependent() || before.Is(TokenType.Data)))
                                {
                                    walker.Remove(next);
                                    yield return new Node(token, NodeType.Call);
                                    yield break;
                                }
                            }
                        }

                        yield return new Node(token.Clone(TokenType.IdentifierKeyword), NodeType.Call);
                        yield break;
                    }
                }
            }

            foreach (var node in nodes)
                yield return node;
        }
        protected override IEnumerable<Node> ParseFacilitatorToken(Token token, TokenWalker walker)
        {
            yield return new Node(token, NodeType.Procedure, SeparatedParseToken(walker).TrimSeparators());
        }
        protected override IEnumerable<Node> ParseCommentToken(Token token, TokenWalker walker)
        {
            bool isblock = token.Value.Contains("\n") || token.Value.StartsWith("/*");
            yield return new Node(new Token(TokenType.Unknown), isblock ? NodeType.Procedure : NodeType.Block,
                new Node(token.Clone(Value: isblock ? token.Value : token.Value), NodeType.Helper),
                walker.Is(TokenType.EndScope) ? null : SeparatedParseToken(walker)
            );
        }
        protected override IEnumerable<Node> ParseSymbolToken(Token token, TokenWalker walker)
        {
            var next = walker.PeekProcedure();
            var next2 = walker.PeekProcedure(1);
            switch (token.Value.ToLower())
            {
                case "be":
                case "is":
                    if (next?.IsMatch("not") == true)
                    {
                        if (next2?.IsMatch("equal", "equals", "be", "===") == true)
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
                    else if (next?.IsMatch("equal", "equals", "be", "===") == true)
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
                    if (next?.IsMatch("to") == true || next?.Is(TokenType.SeparatorSymbol) == true)
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
            if (next?.Is(TokenType.SeparatorSymbol, TokenType.TerminatorSymbol) == true)
                if (token.Is(TokenType.SeparatorSymbol, TokenType.TerminatorSymbol))
                {
                    TargetLocation = Locality - 1;
                    return new Node[] { new Node() };
                }
                else return new Node[] { new Node(token, NodeType.Plain) };
            else if (next?.Is(TokenType.Symbol) == true)
                return ParseSymbolToken(token.Clone(token.Type | next.Type, token.Value + next.Value, null), walker.MoveToProcedure());
            else if (token.Is(TokenType.SeparatorSymbol, TokenType.TerminatorSymbol))
                if (next?.Is(TokenType.Structure | TokenType.Keyword) == true)
                    return new Node[] { new Node() };
                else if (next?.IsDependent() != true && next?.IsIndependent() == true)
                    return new Node[] { new Node(token, NodeType.Plain) };
            return base.ParseSymbolToken(token, walker);
        }


        public static TokenWalker FitWalker(TokenWalker walker)
        {
            return walker.Is(TokenType.SeparatorSymbol, TokenType.TerminatorSymbol) ? walker.MoveToProcedure() : walker;
        }

        public static Node CreateNode(string value = "", params Node[] children) =>
            CreateNode(value, NodeType.Plain, TokenType.Unknown, children);
        public static Node CreateNode(string value, TokenType tokenType, NodeType nodeType = NodeType.Plain, params Node[] children) =>
            CreateNode(value, nodeType, tokenType, children);
        public static Node CreateNode(string value, NodeType nodeType, TokenType tokenType = TokenType.Unknown, params Node[] children)
        {
            return new Node(new Token(tokenType, value), nodeType, children);
        }
        public static Node CreatePackNode(params Node[] children)
        {
            return new Node(new Token(TokenType.Scope, "("), NodeType.Block, children.ToArray());
        }
        public static Node CreateLineNode(Node node)
        {
            if (node.LastLeaf.Token.Is(TokenType.SeparatorSymbol, TokenType.TerminatorSymbol))
                node.LastLeaf.Token.Update(TokenType.TerminatorSymbol, ";");
            else if (!node.Is(NodeType.Block))
                return CreateNode("", node, CreateNode(";", TokenType.TerminatorSymbol));
            return node;
        }
        public static Node CreateBlockNode(params Node[] children)
        {
            return new Node(new Token(TokenType.StartScope, "{"), NodeType.Block, children.ToArray());
        }
        public static Node CreatePackOrBlockNode(params Node[] children)
        {
            children = children.Where(c => !c.Is(NodeType.None)).ToArray();
            if (children.Length == 0) return CreatePackNode();
            if (children.Length > 1) return CreateBlockNode(children);
            if (children[0].Is(NodeType.Block, NodeType.ShortSelector)) return children[0];
            if (children[0].Is(NodeType.Rule)) return CreateBlockNode(children[0]);
            return CreatePackNode(children[0].TrimSeparators());
        }
        public static Node CreateCallNode(Node node, params Node[] args)
        {
            return CreateNode("", node.TrimSeparators(), CreatePackNode(args));
        }
        public static Node CreateCallFunctionNode(string name, params Node[] args)
        {
            return new Node(new Token(TokenType.FunctionKeyword, name), NodeType.Call, args);
        }
        public static Node CreateDefineIdentifierNode(string name, Node value, string state = "var")
        {
            if(string.IsNullOrEmpty(state)) return new Node(new Token(TokenType.IdentifierKeyword, name), NodeType.Procedure,
                CreateNode("="),
                value
            );
            else return new Node(new Token(TokenType.Statement, state), NodeType.Define,
                CreateNode(name, NodeType.Call, TokenType.IdentifierKeyword),
                CreateNode("="),
                value
            );
        }
        public static Node CreateDefineFunctionNode(string name, Node body, params Node[] args)
        {
            return new Node(new Token(TokenType.FunctionKeyword, name), NodeType.Define, 
                CreatePackNode(args),
                body
            );
        }
        public static Node CreateProceduresNode(string value = "", params Node[] children)
        {
            return new Node(new Token(TokenType.Unknown, value), NodeType.Procedure, children.ToArray());
        }
        public static Node CreateCallableNode(Node body, params Node[] args)
        {
            return CreatePackNode(new Node(new Token(TokenType.FunctionKeyword, ""), NodeType.Define, CreatePackNode(args), CreatePackOrBlockNode(body)));
        }
        public static Node CreateNamespaceNode(string ns, Node node)
        {
            return CreateNode(ns + ".", NodeType.Plain, TokenType.ConcatenatorSymbol, node);
        }
    }
}
