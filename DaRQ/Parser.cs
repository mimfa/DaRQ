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

        public int BreakParentSwitch { get; set; } = 0;
        public int BreakCollectSwitch { get; set; } = 0;

        public string[] EqualsSign = new string[] { "=", "=>", "->", ":" };


        public override bool Initialize(MiMFa.Compiler.Compiler compiler)
        {
            if (compiler != null) base.Initialize(Compiler = compiler == null ? Compiler : compiler as Compiler);
            return true;
        }

        public override IEnumerable<Node> Parse(TokenWalker walker, MiMFa.Compiler.Compiler compiler = null)
        {
            BreakParentSwitch = BreakCollectSwitch = 0;
            return base.Parse(walker, compiler);
        }

        protected virtual Node QueryParseToken(TokenWalker walker)
        {
            return CreateLineNode(BlockParseToken(walker));
        }

        protected override Node BlockParseToken(TokenWalker walker)
        {
            return base.BlockParseToken(TrimSeparators(walker));
        }
        protected override IEnumerable<Node> BlockParseTokens(TokenWalker walker)
        {
            var en = base.SequenceParseTokens(walker).GetEnumerator();
            do
            {
                if (BreakParentSwitch > 0)
                {
                    BreakParentSwitch--;
                    break;
                }
                if (en.MoveNext()) yield return en.Current;
                else if (walker.Current != null)
                {
                    var next = walker.PeekProcedure();
                    if (next != null && !IsFlag(next))
                        if (IsComplementors(next))
                            foreach (var node in BlockParseTokens(walker))
                                yield return node;
                        else if (next.Is(TokenType.Symbol))
                            foreach (var node in BlockParseTokens(walker))
                                yield return node;
                    break;
                }
                else break;
            }
            while (true);
        }

        protected override Node SequenceParseToken(TokenWalker walker)
        {
            return TrimSeparators(base.SequenceParseToken(TrimSeparators(walker)));
        }
        protected override IEnumerable<Node> SequenceParseTokens(TokenWalker walker)
        {
            var en = base.CompactParseTokens(walker).GetEnumerator();
            do
            {
                if (BreakCollectSwitch > 0)
                {
                    BreakCollectSwitch--;
                    break;
                }
                if (BreakParentSwitch > 0)
                    break;
                if (en.MoveNext()) yield return en.Current;
                else if (walker.Current != null)
                {
                    var next = walker.PeekProcedure();
                    var next2 = walker.PeekProcedure(1);
                    if (next != null && !IsFinalizers(next) && !IsFlag(next))
                    {
                        if (IsMediators(next) || IsComplementors(next))
                            foreach (var node in SequenceParseTokens(walker))
                                yield return node;
                        else if (
                            IsDelimiters(next) &&
                            !IsComplementors(next2) &&
                            !IsFinalizers(next2) &&
                            !IsOrganizers(next2))
                            foreach (var node in SequenceParseTokens(walker))
                                yield return node;
                    }
                    break;
                }
                else break;
            }
            while (true);
        }

        protected override Node CompactParseToken(TokenWalker walker)
        {
            return TrimSeparators(base.CompactParseToken(TrimSeparators(walker)));
        }
        protected override IEnumerable<Node> CompactParseTokens(TokenWalker walker)
        {
            var en = base.ParseTokens(walker).GetEnumerator();
            do
            {
                if (BreakCollectSwitch > 0)
                    break;
                if (BreakParentSwitch > 0)
                    break;
                if (en.MoveNext()) yield return en.Current;
                else if (walker.Current != null)
                {
                    var next = walker.PeekProcedure();
                    if (next != null && !IsSeparators(next) && !IsFinalizers(next) && !IsFlag(next))
                        if (IsConnectors(next) || IsMediators(next) || IsComplementors(next))
                            yield return CompactParseToken(walker);
                        else if (
                            en.Current != null &&
                            en.Current.Token.Is(TokenType.Keyword) &&
                            !en.Current.Is(NodeType.Define) &&
                            next.Is(TokenType.Keyword) &&
                            !IsSeparators(en.Current.LastLeaf.Token) &&
                            !IsOrganizers(next)
                        )
                        {
                            yield return CreateNode(".");
                            yield return CompactParseToken(walker);
                        }
                    break;
                }
                else break;
            }
            while (true);
        }

        
        protected override IEnumerable<Node> ParseStructureToken(Token token, TokenWalker walker)
        {
            var before = walker.PeekProcedure(-2);
            var next = walker.PeekProcedure();
            var next2 = walker.PeekProcedure(1);

            switch (token.Value.ToLower())
            {
                case "#":
                    var name = walker.Walk().Value;
                    if (IsDelimiters(next2) || next2?.IsMatch(EqualsSign) == true) walker.Remove(next2);

                    yield return Compiler?.SetActionCommand(name,
                         CreateLineNode(CreateCallNode(
                                CreatePackNode(
                                    CreateDefineIdentifierNode(name, 
                                        CreateCallableNode(
                                            QueryParseToken(walker)
                                        ), null
                                    )
                                )
                           )
                        ).Update(type: NodeType.Plain | NodeType.Section)
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
                        yield return Compiler?.SetFunctionCommand(cname.Value, new Node(cname.Clone(TokenType.FunctionKeyword, null, null), NodeType.Define | NodeType.Section,
                            SingleParseToken(walker),
                            CreateBlockNode(BlockParseToken(walker))
                        ));
                    }
                    else
                    {
                        if (next.Is(TokenType.Middle | TokenType.Symbol))
                            walker.Walk();
                        yield return Compiler?.SetDefinitionCommand(cname.Value, CreateNode("var", NodeType.Define | NodeType.Section, TokenType.Structure,
                                new Node(cname.Clone(TokenType.IdentifierKeyword, null, null), NodeType.Call),
                                CreateNode("="),
                                QueryParseToken(walker)
                            )
                        );
                    }
                    yield break;

                case "implements":
                case "extends":
                    if (IsDelimiters(next)) walker.Walk();
                    yield return new Node(token, NodeType.Define, new Node(walker.Walk(), NodeType.Plain), SingleParseToken(walker));
                    yield break;

                case "do":
                case "begin":
                case "doing":
                    if (token.IsMatch("do") && next?.IsMatch("{") == true) break;
                    if (IsDelimiters(next)) walker.Walk();

                    Locality++;
                    var doChildren = new List<Node>();
                    while (walker.IsRunning && !walker.Current.IsMatch("end"))
                    {
                        var n = QueryParseToken(walker);
                        if (!n.Is(NodeType.None)) doChildren.Add(n);
                    }
                    if(walker.IsRunning && walker.Current.IsMatch("end")) walker.Walk();
                    Locality--;
                    if (IsSeparators(walker.Current))
                    {
                        BreakCollectSwitch++;
                        walker.Walk();
                    }
                    if (token.IsMatch("doing"))
                        yield return CreateCallableNode(CreateBlockNode(doChildren.ToArray()));
                    else yield return CreateBlockNode(doChildren.ToArray());
                    yield break;

                case "if":
                    if (next?.IsMatch("(") == true) break;
                    if (IsDelimiters(next)) walker.Walk();

                    var cond = CompactParseToken(walker);
                    var onTrue = QueryParseToken(walker);
                    var onFalse = (TrimSeparators(walker).PeekProcedure()?.IsMatch("else") == true) ? QueryParseToken(walker.MoveToProcedure()) : null;
                    bool isnormal = onFalse == null || (
                        IsGlobalNeeder(onTrue) ||
                        IsGlobalNeeder(onFalse) ||
                        before == null ||
                        before.IsMatch("{", "do", "begin", "end", "else") ||
                        before.Is(TokenType.TerminatorSymbol, TokenType.End | TokenType.Scope)
                    );
                    if(isnormal) yield return new Node(
                        token,
                        NodeType.NormalSelector,
                        cond, CreateLineNode(onTrue), CreateLineNode(onFalse)
                    ); 
                    else yield return CreatePackNode(new Node(
                        token,
                        NodeType.ShortSelector,
                        cond, TrimSeparators(onTrue), TrimSeparators(onFalse)
                    ));
                    yield break;

                case "for":
                    if (next?.IsMatch("(") == true) break;
                    if (IsDelimiters(next)) walker.Walk();
                    if (walker.PeekProcedure(1)?.IsMatch("of", "in") == true || walker.PeekProcedure(2)?.IsMatch("of", "in") == true)
                        yield return new Node(token, NodeType.CollectionIterator,
                            CompactParseToken(walker),
                            QueryParseToken(walker)
                        );
                    else yield return new Node(token, NodeType.ComputationIterator,
                        new Node(null, NodeType.Plain,
                        CreateNode("", TrimSeparators(QueryParseToken(walker)), CreateNode(";", TokenType.TerminatorSymbol)),
                        CreateNode("", TrimSeparators(QueryParseToken(walker)), CreateNode(";", TokenType.TerminatorSymbol)),
                        TrimSeparators(QueryParseToken(walker))),
                        QueryParseToken(walker)
                    );
                    yield break;

                case "while":
                    if (next?.IsMatch("(") == true) break;
                    if (IsDelimiters(next)) walker.Walk();

                    yield return new Node(token, NodeType.ConditionIterator, 
                        new Node(null, NodeType.Plain, CompactParseToken(walker)),
                        QueryParseToken(walker)
                    );
                    yield break;

                case "try":
                    if (next?.IsMatch("(") == true) break;
                    if (IsDelimiters(next)) walker.Walk();

                    var children = new List<Node>();
                    while (walker.Current != null && !walker.Current.IsMatch("catch", "finally"))
                    {
                        var n = QueryParseToken(walker);
                        if (!n.Is(NodeType.None)) children.Add(n);
                    }
                    yield return CreateProceduresNode(token.Value, CreateBlockNode(children.ToArray()));
                    yield break;
                case "catch":
                    if (IsDelimiters(next)) walker.Walk();

                    yield return CreateProceduresNode(token.Value,
                        next?.Is(TokenType.Keyword) == true && IsDelimiters(next2)
                            ? new[] {
                                TrimSeparators(CreatePackNode(CompactParseToken(walker))),
                                CreateBlockNode(BlockParseToken(walker.MoveToProcedure())),
                                walker.PeekProcedure()?.IsMatch("finally", "catch") == true ? SingleParseToken(walker) : null
                            } :
                            (next.IsMatch("(") ? new[] {
                                SingleParseToken(walker),
                                CreateBlockNode(BlockParseToken(walker)),
                                walker.PeekProcedure()?.IsMatch("finally", "catch") == true ? SingleParseToken(walker) : null
                            } : new[] {
                                CreateBlockNode(BlockParseToken(walker)),
                                walker.PeekProcedure()?.IsMatch("finally", "catch") == true ? SingleParseToken(walker) : null}
                            )
                    );
                    yield break;
                case "finally":
                    if (next?.IsMatch("(") == true) break;
                    if (IsDelimiters(next)) walker.Walk();

                    yield return CreateProceduresNode(token.Value, CreateBlockNode(BlockParseToken(walker)), walker.PeekProcedure()?.IsMatch("finally", "catch") == true ? SingleParseToken(walker) : null);
                    yield break;
            }
            
            foreach(var node in base.ParseStructureToken(token, walker))
                yield return node;
        }
        protected override IEnumerable<Node> ParseStartToken(Token token, TokenWalker walker)
        {
            var next = walker.PeekProcedure();
            var next2 = walker.PeekProcedure(1);
            switch (token.Value.ToLower())
            {
                case "will":
                    token.Update(TokenType.FunctionKeyword, "new Promise");
                    if (next == null) yield return new Node();
                    else if (next.IsMatch("("))
                        yield return new Node(token, NodeType.Call, SingleParseToken(walker));
                    else if (next.Is(TokenType.Keyword) && (IsDelimiters(next2) || IsComplementors(next2) || IsOrganizers(next2)))
                        yield return new Node(token, NodeType.Call, CompactParseToken(walker));
                    else
                    {
                        var child = CompactParseToken(walker);
                        if (child != null)
                            if (child.Is(NodeType.Block)) child = CreateCallableNode(child);
                            else if (!child.Is(NodeType.Call) || child.Count > 0) child = CreateCallableNode(child);
                            else child = TrimSeparators(child);
                        yield return new Node(token, NodeType.Call, child);
                    }
                    yield break;
            }
            foreach (var node in base.ParseStartToken(token, walker))
                yield return node;
        }
        protected override IEnumerable<Node> ParseMiddleToken(Token token, TokenWalker walker)
        {
            switch (token.Value.ToLower())
            {
                case "as":
                    var alias = walker.PeekProcedure();
                    if (alias != null)
                    {
                        var aliasValue = alias.Value;
                        walker.Walk();
                        var value = CompactParseToken(walker);
                        yield return CreateNode(aliasValue, NodeType.Compute, TokenType.ObjectData, value);
                    }
                    yield return CreateNode("", NodeType.Compute, TokenType.ObjectData);
                    yield break;
            }

            foreach (var node in base.ParseMiddleToken(token, walker))
                yield return node;
        }
        protected override IEnumerable<Node> ParseSuffixToken(Token token, TokenWalker walker)
        {
            var before = walker.PeekProcedure(-2);
            var next = walker.PeekProcedure();
            var next2 = walker.PeekProcedure(1);
            string dot = before == null || before.Value != "." ? "." : null;

            switch (token.Value.ToLower())
            {
                case "then":
                case "otherwise":
                case "anyway":
                    token.Update(TokenType.FunctionKeyword, token.IsMatch("otherwise") ? $"{dot}catch" : token.IsMatch("anyway") ? $"{dot}finally" : $"{dot}then");
                    if (next == null) yield return new Node();
                    else if (next.IsMatch("("))
                        yield return new Node(token, NodeType.Call, SingleParseToken(walker));
                    else if (next.Is(TokenType.Keyword) && (IsSeparators(next2) || IsComplementors(next2) || IsFinalizers(next2) || IsOrganizers(next2)))
                        yield return new Node(token, NodeType.Call, CompactParseToken(walker));
                    else
                    {
                        var child = CompactParseToken(walker);
                        if (child != null)
                            if (child.Is(NodeType.Block))
                                child = CreateCallableNode(CreateLineNode(child.Insert(0, CreateNode("handlers(data);"))), CreateNode("data", NodeType.Plain, TokenType.Keyword));
                            else if (!child.Is(NodeType.Call) || child.Count > 0)
                                child = CreateCallableNode(CreateBlockNode(CreateNode("handlers(data);"), CreateLineNode(child)), CreateNode("data", NodeType.Plain, TokenType.Keyword));
                            else child = TrimSeparators(child);
                        yield return new Node(token, NodeType.Call, child);
                    }
                    yield break;

                case "where":
                    token.Update(TokenType.FunctionKeyword, "filter");
                    yield return new Node(token, NodeType.Call, 
                        CreateCallableNode(CompactParseToken(walker), CreateNode("data"))
                    );
                    yield break;

                case "distinct":
                    token.Update(TokenType.FunctionKeyword, "filter");
                    yield return new Node(token, NodeType.Call,
                        CreateCallableNode(CreateProceduresNode("", CreateNode("self.indexOf(data) === index"), CreateNode("data")), CreateNode("data"))
                    );
                    yield break;

                case "limit":
                    if (IsDelimiters(next)) walker.Walk();
                    token.Update(TokenType.FunctionKeyword, "slice");
                    var nlimit = SequenceParseToken(walker);
                    yield return new Node(token, NodeType.Call,
                        nlimit.Count > 1 ? nlimit.Children.ToArray() : new[] { CreateNode("0", NodeType.Plain, TokenType.NumberData), nlimit });
                    yield break;

                case "order":
                    if (IsDelimiters(next)) walker.Walk();
                    var norders = SequenceParseToken(walker);
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

                case "keys":
                case "values":
                    yield return CreateNode("", new Node(token.Update(TokenType.FunctionKeyword, dot + token.Value), NodeType.Call), CreateCallFunctionNode(".toArray"));
                    yield break;

                default:
                    break;
            }
            
            foreach (var node in base.ParseSuffixToken(token, walker))
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
                string fname = Compiler?.GetFunctionName(node.Token.Value);
                string iname = fname !=null?null:Compiler?.GetKeywordName(node.Token.Value);
                string fcname = Compiler?.GetFunctionCommandName(node.Token.Value);
                string dcname = Compiler?.GetDefinitionCommandName(node.Token.Value);
                string acname = Compiler?.GetActionCommandName(node.Token.Value);
                fname = fname ?? fcname;
                if (node.Token.Is(TokenType.FunctionKeyword))
                {
                    if (before?.Is(TokenType.ConcatenatorSymbol) == true) yield return node;
                    else
                    {
                        node.Token.Update(TokenType.FunctionKeyword, acname ?? fname ?? node.Token.Value);
                        foreach (var n in nodes)
                            yield return n;
                        if (!string.IsNullOrEmpty(acname) &&
                                next != null &&
                                next.Is(TokenType.Keyword)
                            )
                            yield return new Node(token.Clone(TokenType.NamespaceKeyword), NodeType.Call, CreateNode(IsComplementors(next)?"":".", NodeType.Plain, TokenType.ConcatenatorSymbol), CompactParseToken(walker));
                    }
                    yield break;
                }
                else if (node.Token.Is(TokenType.IdentifierKeyword))
                {
                    if (acname != null) yield return CreateNode(acname, NodeType.Procedure, TokenType.Data);
                    else
                    {
                        token.Update(TokenType.IdentifierKeyword, fname ?? dcname ?? iname ?? node.Token.Value);
                        if (
                            next?.Is(TokenType.Keyword, TokenType.Data, TokenType.Start | TokenType.Scope) == true &&
                            !IsSeparators(next) &&
                            !IsMediators(next) &&
                            !IsComplementors(next) &&
                            !IsFinalizers(next) &&
                            !IsOrganizers(next) &&
                            (
                                fname != null ||
                                next.Is(TokenType.Data) ||
                                next.IsMatch("(", "...") ||
                                IsSeparators(next2) ||
                                IsMediators(next2) ||
                                IsComplementors(next2) ||
                                IsFinalizers(next2) ||
                                IsOrganizers(next2)
                            )
                        )
                        {
                            yield return new Node(
                                token.Clone(TokenType.FunctionKeyword),
                                NodeType.Call,
                                SequenceParseToken(walker)
                            );
                            yield break;
                        }
                        else if (next.Is(TokenType.Keyword))
                        {
                            yield return new Node(token.Clone(TokenType.NamespaceKeyword), NodeType.Call, CreateNode(IsComplementors(next) ? "" : ".", NodeType.Plain, TokenType.ConcatenatorSymbol), CompactParseToken(walker));
                            yield break;
                        }
                        else if (fcname != null && (IsSeparators(next) || IsComplementors(next) || IsMediators(next) || IsFinalizers(next) || IsOrganizers(next)))
                        {
                            yield return CreateCallFunctionNode(fcname);
                            yield break;
                        }
                        else if (IsDelimiters(next))
                        {
                            if (IsOrganizers(before))
                            {
                                walker.Remove(next);
                                yield return new Node(token, NodeType.Call);
                                yield break;
                            }
                            else if (next2?.Is(TokenType.Keyword) == true || IsConnectors(next2) || IsComplementors(next2) || IsMediators(next2) || IsFinalizers(next2) || IsOrganizers(next2))
                            {
                                yield return new Node(token, NodeType.Call);
                                yield break;
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
        protected override IEnumerable<Node> ParseCommentToken(Token token, TokenWalker walker)
        {
            bool isblock = token.Value.Contains("\n") || token.Value.StartsWith("/*");
            yield return new Node(new Token(TokenType.Unknown), isblock ? NodeType.Procedure : NodeType.Block,
                new Node(token.Clone(Value: isblock ? token.Value : token.Value), NodeType.Helper),
                walker.Is(TokenType.End | TokenType.Scope) ? null : CompactParseToken(walker)
            );
        }
        protected override IEnumerable<Node> ParseSymbolToken(Token token, TokenWalker walker)
        {
            var before = walker.PeekProcedure(-2);
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
                            return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "!==", null), walker);
                        }
                        else if (next2?.Is(TokenType.StringData) == true && next2?.IsMatch("", "empty") == true)
                        {
                            walker.MoveToProcedure(); walker.MoveToProcedure();
                            return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "+ \"\" != \"\"", null), walker);
                        }
                        else if (next?.IsMatch("==", "=") == true)
                        {
                            walker.MoveToProcedure(); walker.MoveToProcedure();
                            return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "!=", null), walker);
                        }
                        else { walker.MoveToProcedure(); return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "!=", null), walker); }
                    }
                    else if (next?.IsMatch("equal", "equals", "be", "===") == true)
                    {
                        walker.MoveToProcedure(); return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "===", null), walker);
                    }
                    else if (next?.Is(TokenType.StringData) == true && next?.IsMatch("", "empty") == true)
                    {
                        walker.MoveToProcedure(); return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "+ \"\" == \"\"", null), walker);
                    }
                    else return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "==", null), walker);

                case "equal":
                case "equals":
                    if (IsDelimiters(next))
                    {
                        walker.MoveToProcedure();
                        return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "===", null), walker);
                    }
                    else return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "===", null), walker);

                case "not":
                    if (next?.IsMatch("equal", "equals", "===") == true)
                    {
                        walker.MoveToProcedure();
                        return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "!==", null), walker);
                    }
                    else if (next?.IsMatch("be", "==", "=") == true)
                    {
                        walker.MoveToProcedure();
                        return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "!=", null), walker);
                    }
                    else if (next?.Is(TokenType.StringData) == true && next?.IsMatch("", "empty") == true)
                    {
                        walker.MoveToProcedure();
                        return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "+ \"\" != \"\"", null), walker);
                    }
                    else return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "!", null), walker);
                default:
                    break;
            }

            if (
                (IsAcceptors(before) || IsDelimiters(before) || IsInitializers(before) || IsOrganizers(before)) &&
                !IsSeparators(token) &&
                (IsSeparators(next) || IsComplementors(next) || IsMediators(next) || IsFinalizers(next) || IsOrganizers(next))
            )
                return ParseDataToken(token.Clone(TokenType.StringData), walker);
            else if (next?.Is(TokenType.Symbol) == true && !IsInitializers(next))
                if (token.Is(TokenType.DelimiterSymbol))
                {
                    BreakCollectSwitch++;
                    return new Node[] { new Node() };
                }
                else if (token.Is(TokenType.TerminatorSymbol))
                {
                    BreakParentSwitch++;
                    return new Node[] { new Node() };
                }
                else return ParseSymbolToken(token.Clone(token.Type | next.Type, token.Value + next.Value, null), walker.MoveToProcedure());
            else if (IsSeparators(token))
            {
                if (IsConnectors(next) || IsComplementors(next) || IsMediators(next) || IsFinalizers(next))
                    return new Node[] { new Node() };
                else if (IsOrganizers(next))
                    return new Node[] { new Node(token.Update(TokenType.TerminatorSymbol, ";"), NodeType.Plain) };
                else if (token.Is(TokenType.DelimiterSymbol)) 
                    return new Node[] { new Node(token, NodeType.Procedure, SequenceParseToken(walker)) };
                else if (token.Is(TokenType.TerminatorSymbol))
                    return new Node[] { new Node(token, NodeType.Procedure) };
            }
            return base.ParseSymbolToken(token, walker);
        }

        public virtual bool IsGlobalNeeder(Node node) => node != null && node.Has(n => n.Token.IsMatch("return", "yield", "{", "do", "begin", "end"));

        public virtual bool IsFlag(Token token) => token != null && token.Is(TokenType.End | TokenType.Scope);

        public virtual bool IsAcceptors(Token token) => token != null && (token.Is(TokenType.FunctionKeyword) || (Compiler?.GetFunctionName(token.Value) ?? Compiler?.GetFunctionCommandName(token.Value) ?? null) != null);
        public virtual bool IsInitializers(Token token) => token != null && token.Is(TokenType.Start, TokenType.Prefix);
        public virtual bool IsConnectors(Token token) => token != null && (token.IsMatch("[", "(") || token.Is(TokenType.Middle | TokenType.Symbol) || token.Is(TokenType.Suffix | TokenType.Symbol));
        public virtual bool IsComplementors(Token token) => token != null && token.Is(TokenType.Suffix, TokenType.ConcatenatorSymbol);
        public virtual bool IsMediators(Token token) => token != null && token.Is(TokenType.Middle);
        public virtual bool IsSeparators(Token token) => token != null && token.Is(TokenType.DelimiterSymbol, TokenType.TerminatorSymbol);
        public virtual bool IsDelimiters(Token token) => token != null && token.Is(TokenType.DelimiterSymbol);
        public virtual bool IsFinalizers(Token token) => token != null && token.Is(TokenType.End, TokenType.TerminatorSymbol);
        public virtual bool IsOrganizers(Token token) => token != null && token.Is(TokenType.Structure, TokenType.TerminatorSymbol);


        public virtual TokenWalker TrimSeparators(TokenWalker walker)
        {
            return walker.Is(TokenType.DelimiterSymbol, TokenType.TerminatorSymbol) ? walker.MoveToProcedure() : walker;
        }
        public virtual Node TrimSeparators(Node node)
        {
            if (node.Is(NodeType.Block) && node.Token.IsMatch("{")) return node;
            return node.Trim(n => n.Count <= 0 && n.Ancestor(p => p.Is(NodeType.Block))?.Token.IsMatch("{") != true && n.Token.Is(TokenType.DelimiterSymbol, TokenType.TerminatorSymbol));
        }

        public virtual Node CreateNode(string value = "", params Node[] children) =>
            CreateNode(value, NodeType.Plain, TokenType.Unknown, children);
        public virtual Node CreateNode(string value, TokenType tokenType, NodeType nodeType = NodeType.Plain, params Node[] children) =>
            CreateNode(value, nodeType, tokenType, children);
        public virtual Node CreateNode(string value, NodeType nodeType, TokenType tokenType = TokenType.Unknown, params Node[] children)
        {
            return new Node(new Token(tokenType, value), nodeType, children);
        }
        public virtual Node CreatePackNode(params Node[] children)
        {
            if (children.Length == 1 && children[0].Is(NodeType.Block) && children[0].Token.IsMatch("(")) return children[0];
            return new Node(new Token(TokenType.Scope, "("), NodeType.Block, children.ToArray());
        }
        public virtual Node CreateLineNode(Node node)
        {
            if (node == null) return CreateNode(";", TokenType.TerminatorSymbol);
            if (node.LastLeaf.Token.Is(TokenType.DelimiterSymbol, TokenType.TerminatorSymbol))
                node.LastLeaf.Token.Update(TokenType.TerminatorSymbol, ";");
            else if (!node.Is(NodeType.Block))
                return CreateNode("", node, CreateNode(";", TokenType.TerminatorSymbol));
            return node;
        }
        public virtual Node CreateBlockNode(params Node[] children)
        {
            if (children.Length == 1 && children[0].Is(NodeType.Block) && children[0].Token.IsMatch("{")) return children[0];
            return new Node(new Token(TokenType.Start | TokenType.Scope, "{"), NodeType.Block, children.ToArray());
        }
        public virtual Node CreatePackOrBlockNode(params Node[] children)
        {
            children = children.Where(c => !c.Is(NodeType.None)).ToArray();
            if (children.Length == 0) return CreatePackNode();
            if (children.Length > 1) return CreateBlockNode(children);
            if (children[0].Is(NodeType.Block, NodeType.ShortSelector)) return children[0];
            if (children[0].Is(NodeType.Rule) || IsGlobalNeeder(children[0])) return CreateBlockNode(children[0]);
            return TrimSeparators(CreatePackNode(children[0]));
        }
        public virtual Node CreateCallNode(Node node, params Node[] args)
        {
            return CreateNode("", TrimSeparators(node), CreatePackNode(args));
        }
        public virtual Node CreateCallFunctionNode(string name, params Node[] args)
        {
            return new Node(new Token(TokenType.FunctionKeyword, name), NodeType.Call, args);
        }
        public virtual Node CreateDefineIdentifierNode(string name, Node value, string state = "var")
        {
            if(string.IsNullOrEmpty(state)) return new Node(new Token(TokenType.IdentifierKeyword, name), NodeType.Procedure,
                CreateNode("="),
                value
            );
            else return new Node(new Token(TokenType.Structure, state), NodeType.Define,
                CreateNode(name, NodeType.Call, TokenType.IdentifierKeyword),
                CreateNode("="),
                value
            );
        }
        public virtual Node CreateDefineFunctionNode(string name, Node body, params Node[] args)
        {
            return new Node(new Token(TokenType.FunctionKeyword, name), NodeType.Define, 
                CreatePackNode(args),
                body
            );
        }
        public virtual Node CreateProceduresNode(string value = "", params Node[] children)
        {
            return new Node(new Token(TokenType.Unknown, value), NodeType.Procedure, children.ToArray());
        }
        public virtual Node CreateCallableNode(Node body, params Node[] args)
        {
            return CreatePackNode(CreatePackNode(args), CreateNode("=>", TokenType.Middle | TokenType.Symbol, NodeType.Procedure, CreatePackOrBlockNode(body)));
        }
        public virtual Node CreateNamespaceNode(string ns, Node node)
        {
            return CreateNode(ns + ".", NodeType.Plain, TokenType.ConcatenatorSymbol, node);
        }
    }
}
