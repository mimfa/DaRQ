using System.Linq;
using MiMFa.Compiler.Model;
using MiMFa.Compiler.Walker;

namespace MiMFa.Compiler.DaRQ
{
    public class Assembler : MiMFa.Compiler.JavaScript.Assembler
    {
        public new Compiler Compiler { get; set; }

        public int BreakParentSwitch { get; set; } = 0;
        public int BreakCollectSwitch { get; set; } = 0;

        public string[] EqualsSign = new string[] { "=", "=>", "->", ":" };


        public virtual bool IsGlobalNeeder(Node node) => node != null && node.Has(n => n.Token.IsMatch("return", "yield", "{", "do", "begin", "end"));
        public virtual bool IsAcceptors(Token token) => token != null && (token.Is(TokenType.FunctionKeyword) || (Compiler?.GetFunctionName(token.Value) ?? Compiler?.GetFunctionCommandName(token.Value) ?? null) != null);


        public virtual TokenWalker TrimSeparators(TokenWalker walker)
        {
            return walker.Is(TokenType.DelimiterSymbol, TokenType.TerminatorSymbol) ? walker.MoveToProcedure() : walker;
        }
        public virtual Node TrimSeparators(Node node)
        {
            if (node.Is(NodeType.BlockStructure) && node.Token.IsMatch("{")) return node;
            return node.Trim(n => n.Count <= 0 && n.Ancestor(p => p.Is(NodeType.BlockStructure))?.Token.IsMatch("{") != true && n.Token.Is(TokenType.DelimiterSymbol, TokenType.TerminatorSymbol));
        }

        public virtual Node CreateNode(string value = "", params Node[] children) =>
            CreateNode(value, NodeType.Chunk, TokenType.Unknown, children);
        public virtual Node CreateNode(string value, TokenType tokenType, NodeType nodeType = NodeType.Chunk, params Node[] children) =>
            CreateNode(value, nodeType, tokenType, children);
        public virtual Node CreateNode(string value, NodeType nodeType, TokenType tokenType = TokenType.Unknown, params Node[] children)
        {
            return new Node(new Token(tokenType, value), nodeType, children);
        }
        public virtual Node CreatePackNode(params Node[] children)
        {
            if (children.Length == 1 && children[0].Is(NodeType.BlockStructure) && children[0].Token.IsMatch("(")) return children[0];
            return new Node(new Token(TokenType.Scope, "("), NodeType.BlockStructure, children.ToArray());
        }
        public virtual Node CreateLineNode(Node node)
        {
            if (node == null) return CreateNode(";", TokenType.TerminatorSymbol);
            if (node.LastLeaf.Token.Is(TokenType.DelimiterSymbol, TokenType.TerminatorSymbol))
                node.LastLeaf.Token.Update(TokenType.TerminatorSymbol, ";");
            else if (!node.Is(NodeType.BlockStructure))
                return CreateNode("", node, CreateNode(";", TokenType.TerminatorSymbol));
            return node;
        }
        public virtual Node CreateBlockNode(params Node[] children)
        {
            if (children.Length == 1 && children[0].Is(NodeType.BlockStructure) && children[0].Token.IsMatch("{")) return children[0];
            return new Node(new Token(TokenType.Start | TokenType.Scope, "{"), NodeType.BlockStructure, children.ToArray());
        }
        public virtual Node CreatePackOrBlockNode(params Node[] children)
        {
            children = children.Where(c => !c.Is(NodeType.None)).ToArray();
            if (children.Length == 0) return CreatePackNode();
            if (children.Length > 1) return CreateBlockNode(children);
            if (children[0].Is(NodeType.BlockStructure, NodeType.ShortSelectorStructure)) return children[0];
            if (children[0].Is(NodeType.Structure) || IsGlobalNeeder(children[0])) return CreateBlockNode(children[0]);
            return TrimSeparators(CreatePackNode(children[0]));
        }
        public virtual Node CreateCallNode(Node node, params Node[] args)
        {
            return CreateNode("", TrimSeparators(node), CreatePackNode(args));
        }
        public virtual Node CreateCallFunctionNode(string name, params Node[] args)
        {
            return new Node(new Token(TokenType.FunctionKeyword, name), NodeType.CallStructure, args);
        }
        public virtual Node CreateDefineIdentifierNode(string name, Node value, string state = "var")
        {
            if (string.IsNullOrEmpty(state)) return new Node(new Token(TokenType.IdentifierKeyword, name), NodeType.Prepend,
                 CreateNode("="),
                 value
             );
            else return new Node(new Token(TokenType.Statement, state), NodeType.DefineStructure,
                CreateNode(name, NodeType.CallStructure, TokenType.IdentifierKeyword),
                CreateNode("="),
                value
            );
        }
        public virtual Node CreateDefineFunctionNode(string name, Node body, params Node[] args)
        {
            return new Node(new Token(TokenType.FunctionKeyword, name), NodeType.DefineStructure,
                CreatePackNode(args),
                body
            );
        }
        public virtual Node CreateProceduresNode(string value = "", params Node[] children)
        {
            return new Node(new Token(TokenType.Unknown, value), NodeType.Prepend, children.ToArray());
        }
        public virtual Node CreateCallableNode(Node body, params Node[] args)
        {
            return CreatePackNode(CreatePackNode(args), CreateNode("=>", TokenType.Middle | TokenType.Symbol, NodeType.Prepend, CreatePackOrBlockNode(body)));
        }
        public virtual Node CreateNamespaceNode(string ns, Node node)
        {
            return CreateNode(ns + ".", NodeType.Chunk, TokenType.ConcatenatorSymbol, node);
        }
    }
}
