using System.Collections.Generic;
using System.Linq;
using MiMFa.Compiler.Model;
using MiMFa.Compiler.Walker;

namespace MiMFa.Compiler.JavaScript
{
    /// <summary>
    /// Hierarchical assembler. It receives independent token nodes and packs
    /// them through the language semantic assembler.
    /// </summary>
    public class Assembler : MiMFa.Compiler.Assembler.Assembler
    {
        public int Location { get; set; } = 0;
        public Node LastNode { get; set; } = null;

        public override IEnumerable<Node> Assemble(NodeWalker walker, MiMFa.Compiler.Compiler compiler = null)
        {
            if (!Initialize(compiler)) yield break;
            while (!walker.IsEnded)
            {
                Location = 0;
                yield return SectionAssembleNode(walker);
            }
        }

        protected virtual Node SectionAssembleNode(NodeWalker walker)
        {
            Location++;
            var nodes = SectionAssembleNodes(walker).ToList();
            Location--;
            if (nodes.Count > 1) return new Node(null, NodeType.Chunk, Location + 1, nodes.ToArray());
            if (nodes.Count == 1) return nodes[0].Update(location: Location + 1);
            return new Node(location: Location + 1);
        }
        protected virtual IEnumerable<Node> SectionAssembleNodes(NodeWalker walker)
        {
            foreach (var node in SequenceAssembleNodes(walker)) yield return node;
                var next = walker.PeekProcedure();
                if (next != null && !(Compiler as Compiler).IsFlag(next))
                    if (next.Is(TokenType.Symbol))
                        foreach (var node in SectionAssembleNodes(walker))
                            yield return node;
        }

        protected virtual Node SequenceAssembleNode(NodeWalker walker)
        {
            Location++;
            var nodes = SequenceAssembleNodes(walker).ToList();
            Location--;
            if (nodes.Count > 1) return new Node(null, NodeType.Chunk, Location + 1, nodes.ToArray());
            if (nodes.Count == 1) return nodes[0].Update(location: Location + 1);
            return new Node(location: Location + 1);
        }
        protected virtual IEnumerable<Node> SequenceAssembleNodes(NodeWalker walker)
        {
            foreach (var node in CompactAssembleNodes(walker)) yield return node;
                var next = walker.PeekProcedure();
                if (next != null && !(Compiler as Compiler).IsFlag(next))
                    if (next.Is(TokenType.DelimiterSymbol))
                        foreach (var node in SequenceAssembleNodes(walker))
                            yield return node;
        }

        protected virtual Node CompactAssembleNode(NodeWalker walker)
        {
            var nodes = CompactAssembleNodes(walker).ToList();
            if (nodes.Count > 1) return new Node(null, NodeType.Chunk, Location, nodes.ToArray());
            if (nodes.Count == 1) return nodes[0].Update(location: Location);
            return new Node(location: Location);
        }
        protected virtual IEnumerable<Node> CompactAssembleNodes(NodeWalker walker)
        {
            yield return AssembleNode(walker);
            var next = walker.PeekProcedure();
            if (next != null && !(Compiler as Compiler).IsFlag(next))
                if (next.Is(
                        TokenType.ConcatenatorSymbol,
                        TokenType.Suffix,
                        TokenType.Middle,
                        TokenType.End,
                        TokenType.Start | TokenType.Scope
                    )
                )
                    foreach (var node in CompactAssembleNodes(walker))
                        yield return node;
        }

        protected virtual Node AssembleNode(NodeWalker walker)
        {
            return AssembleNode(walker.Walk(), walker).Update(location: Location);
        }

        protected override Node AssembleStructureNode(Node node, NodeWalker walker)
        {
            if (node.Is(NodeType.BlockStructure))
                if (node.Token.Is(TokenType.Start | TokenType.Scope))
                {
                    var before = walker.PeekProcedure(-2);
                    if (node.Token.IsMatch("{"))
                    {
                        if (before != null && !before.Is(TokenType.End | TokenType.Scope, TokenType.Statement, TokenType.TerminatorSymbol)) node.Token.Update(TokenType.ObjectData);
                        else node.Token.Update(TokenType.Scope);
                    }
                    else if (node.Token.IsMatch("[")) node.Token.Update(TokenType.ArrayData);

                    Location++;
                    if (node.Token.IsMatch("{") || (before != null && before.IsMatch("for")))
                        while (walker.Current != null && !walker.Current.Is(TokenType.End | TokenType.Scope))
                        {
                            var n = SectionAssembleNode(walker);
                            if (n != null && !n.Is(NodeType.None)) node.Add(n);
                        }
                    else while (walker.Current != null && !walker.Current.Is(TokenType.End | TokenType.Scope))
                        {
                            var n = SequenceAssembleNode(walker);
                            if (n != null && !n.Is(NodeType.None)) node.Add(n);
                        }
                    if (node.Token.IsMatch("{", "[", "(")) AssembleNode(walker);
                    else node.Add(AssembleNode(walker));
                    Location--;
                    return node;
                }
                else if (node.Token.IsMatch("}", "]", ")"))
                    return new Node();
                else if (node.Token.Is(TokenType.End | TokenType.Scope))
                    return node;
                else
                    return node.Update(type: NodeType.BlockStructure).Add(SequenceAssembleNode(walker));
            
            if (node.Is(NodeType.CallStructure))
                if(node.Token.Is(TokenType.NamespaceKeyword))
                    return node.Add(AssembleNode(walker));
                else if (node.Token.Is(TokenType.FunctionKeyword))
                    return node.Add(AssembleNode(walker));
                else return node;

            switch (node.Token.Value)
            {
                case "get":
                case "set":
                    return node.AddRange(AssembleNode(walker), AssembleNode(walker));
                case "function":
                    var fname = walker.PeekProcedure();
                    if (fname != null && fname.Is(TokenType.Keyword))
                    {
                        (Compiler as Compiler).SetKeyword(fname.Token.Update(TokenType.FunctionKeyword));
                        walker.Replace(fname);
                    }
                    return node.AddRange(AssembleNode(walker), AssembleNode(walker));

                case "implements":
                case "extends":
                    return node.AddRange(AssembleNode(walker), AssembleNode(walker));

                case "interface":
                case "class":
                case "enum":
                case "package":
                default:
                    var cname = walker.PeekProcedure();
                    if (cname.Is(TokenType.Keyword))
                    {
                        (Compiler as Compiler).SetKeyword(cname.Token.Update(TokenType.IdentifierKeyword));
                        walker.Replace(cname);
                    }
                    return node.AddRange(AssembleNode(walker), AssembleNode(walker));

                case "if":
                    return node.AddRange(AssembleNode(walker), SectionAssembleNode(walker), (walker.PeekProcedure()?.Token?.IsMatch("else") == true) ? SectionAssembleNode(walker) : null);
                case "else":
                    return node.AddRange(SectionAssembleNode(walker));

                case "switch":
                    return node.AddRange(AssembleNode(walker), AssembleNode(walker));
                case "case":
                    node.Add(AssembleNode(walker)).Update(type: node.Type | NodeType.BlockStructure);
                    return node.AddRange(walker.MapUntil(() => 
                        walker.Current == null ||
                        walker.Current.IsMatch("case", "default") ||
                        walker.Current.Is(TokenType.End | TokenType.Scope),
                    () => SectionAssembleNode(walker)).ToArray());
                case "default":
                    node.Add(AssembleNode(walker)).Update(type: node.Type | NodeType.BlockStructure);
                    return node.AddRange(walker.MapUntil(() =>
                        walker.Current == null ||
                        walker.Current.IsMatch("case", "default") ||
                        walker.Current.Is(TokenType.End | TokenType.Scope),
                    () => SectionAssembleNode(walker)).ToArray());

                case "for":
                    return node.AddRange(AssembleNode(walker), SectionAssembleNode(walker));

                case "while":
                    return node.AddRange(AssembleNode(walker), SectionAssembleNode(walker));

                case "do":
                    return node.AddRange(AssembleNode(walker), SectionAssembleNode(walker));

                case "break":
                    return node.Add(AssembleNode(walker));
                case "continue":
                    return node.Add(SectionAssembleNode(walker));

                case "try":
                    return node.AddRange(AssembleNode(walker), AssembleNode(walker));
                case "catch":
                    return node.AddRange(
                        walker.PeekProcedure()?.IsMatch("(") == true ? AssembleNode(walker) : null,
                        AssembleNode(walker),
                        walker.PeekProcedure()?.IsMatch("finally", "catch") == true ? AssembleNode(walker) : null
                    );
                case "finally":
                    return node.AddRange(
                        AssembleNode(walker),
                        walker.PeekProcedure()?.IsMatch("finally", "catch") == true ? AssembleNode(walker) : null
                        );

                case "return":
                case "yield":
                case "throw":
                    return node.Add(SectionAssembleNode(walker));

                case "import":
                case "export":
                    return node.Add(SectionAssembleNode(walker));

                case "void":
                    return node.Add(AssembleNode(walker));

                case "var":
                case "let":
                case "const":
                    return node.Add(SectionAssembleNode(walker));

                case "with":
                case "debugger":
                    return node.Add(SectionAssembleNode(walker));

                case "delete":
                    return node.Add(SectionAssembleNode(walker));

                case "await":
                case "async":
                case "new":
                    return node.Add(AssembleNode(walker));

                case "private":
                    var n1 = SectionAssembleNode(walker);
                    n1.AccessType |= AccessType.Private;
                    return node.Add(n1);
                case "protected":
                    var n2 = SectionAssembleNode(walker);
                    n2.AccessType |= AccessType.Protected;
                    return node.Add(n2);
                case "internal":
                    var n3 = SectionAssembleNode(walker);
                    n3.AccessType |= AccessType.Internal;
                    return node.Add(n3);
                case "public":
                    var n4 = SectionAssembleNode(walker);
                    n4.AccessType |= AccessType.Public;
                    return node.Add(n4);
                case "static":
                    var n5 = SectionAssembleNode(walker);
                    n5.AccessType |= AccessType.Global;
                    return node.Add(n5);
            }
        }
        protected override Node AssembleProgramNode(Node node, NodeWalker walker)
        {
            return node;
        }
        protected override Node AssembleRegionNode(Node node, NodeWalker walker)
        {
            return !(Compiler as Compiler).IsPrependent(node) || (Compiler as Compiler).IsFinalizers(node) || (Compiler as Compiler).IsFlag(walker.PeekProcedure()) ? node : node.AddRange(SectionAssembleNodes(walker).ToArray());
        }
        protected override Node AssembleLineNode(Node node, NodeWalker walker)
        {
            return !(Compiler as Compiler).IsPrependent(node) || (Compiler as Compiler).IsFinalizers(node) || (Compiler as Compiler).IsFlag(walker.PeekProcedure()) ? node : node.AddRange(SequenceAssembleNodes(walker).ToArray());
        }
        protected override Node AssembleChunkNode(Node node, NodeWalker walker)
        {
            return !(Compiler as Compiler).IsPrependent(node) || (Compiler as Compiler).IsFinalizers(node) || (Compiler as Compiler).IsFlag(walker.PeekProcedure()) ? node : node.AddRange(CompactAssembleNodes(walker).ToArray());
        }
        protected override Node AssembleIndependNode(Node node, NodeWalker walker)
        {
            return node;
        }
        protected override Node AssembleDependNode(Node node, NodeWalker walker)
        {
            if (node.Token.IsMatch("="))
                (Compiler as Compiler).SetKeyword(LastNode?.Seek(n=>n.Token.Is(TokenType.Keyword))?.Token);
            return AssemblePrependNode(node, walker);
        }
        protected override Node AssembleAppendNode(Node node, NodeWalker walker)
        {
            if ((Compiler as Compiler).IsSeparators(node))
                if (walker.PeekProcedure()?.Is(TokenType.Comment) == true &&
                    System.Text.RegularExpressions.Regex.IsMatch(walker.PeekProcedure().Token.Value+"", "^\\s*\\/{2}"))
                    node.Add(walker.Walk());
            return node;
        }
        protected override Node AssemblePrependNode(Node node, NodeWalker walker)
        {
            var child = CompactAssembleNode(walker);
            if ((Compiler as Compiler).IsPrependent(node) && (Compiler as Compiler).IsAppendent(child)) return node.Add(child);
            else return new Node(new Token(), NodeType.Region, node, child);
        }
        protected override Node AssembleUnknownNode(Node node, NodeWalker walker)
        {
            return node;
        }
    }
}
