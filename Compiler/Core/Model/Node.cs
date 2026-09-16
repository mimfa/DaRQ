// Converted from src/engine/DaRQ/Compiler/Core/Node.ts
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiMFa.Compiler.Model
{
    public class Node
    {
        public Token Token { get; set; }
        public NodeType Type { get; set; }
        public Node Parent { get; protected set; }

        protected List<Node> children = new List<Node>();
        public IList<Node> Children
        {
            get => children;
            set
            {
                children.Clear();
                foreach (var v in value) Add(v);
            }
        }

        public AccessType AccessType { get; set; }
        public int Location { get; set; } = -1;

        public int Count => children.Count;
        public Node ForceFirst
        {
            get => First??new Node();
            set
            {
                if (children.Count > 0) children[0] = value;
                else Add(value);
            }
        }
        public Node ForceLast
        {
            get => Last ?? new Node();
            set
            {
                if (children.Count > 0) children[children.Count - 1] = value;
                else Add(value);
            }
        }
        public Node First => children.Count > 0 ? children.FirstOrDefault(v => !v.IsEmpty()) : null;
        public Node Last => children.Count > 0 ? children.LastOrDefault(v=>!v.IsEmpty()) : null;

        public Node FirstLeaf => First != null ? First.FirstLeaf : this;
        public Node LastLeaf => Last != null ? Last.LastLeaf : this;

        public Node(Token token = null, NodeType? type = null, int location = -1, params Node[] children) : this(token, type, AccessType.Unknown, location, children) { }
        public Node(Token token = null, NodeType? type = null, params Node[] children) : this(token, type, AccessType.Unknown, -1, children) { }
        public Node(Token token = null, NodeType? type = null, AccessType accessType = AccessType.Unknown, int location = -1, IEnumerable<Node> children = null, Node parent = null)
        {
            Token = token ?? new Token();
            Type = type ?? (token != null ? NodeType.Unknown : NodeType.None);
            Parent = parent;
            Children = children == null? new List<Node>() : children.ToList() ?? new List<Node>();
            Location = location;
            AccessType = accessType;
        }

        public Node Update(Token token = null, NodeType? type = null, AccessType? accessType = null, int? location = null, IEnumerable<Node> children = null, Node parent = null)
        {
            Token = token ?? Token;
            Type = type ?? Type;
            Parent = parent ?? parent;
            Children = (children ?? Children).ToList();
            Location = location ?? Location;
            AccessType = accessType ?? AccessType;
            return this;
        }

        public Node Clone(Token token = null, NodeType? type = null, AccessType? accessType = null, int? location = null, IEnumerable<Node> children = null, Node parent = null)
        {
            return new Node(token ?? Token, type?? Type, accessType??AccessType, location??Location, children?.Select(c => c.Clone()).ToList(), parent ?? parent);
        }

        public bool Is(params NodeType[] nodeTypes)
        {
            foreach (var nt in nodeTypes)
                if (((int)Type & (int)nt) == (int)nt) return true;
            return false;
        }
        public bool Has(Func<Node, bool> condition)
        {
            return condition(this) || Children.Any(n=>n.Has(condition));
        }

        public bool IsEmpty() => Is(NodeType.None, NodeType.Unknown) && Token.Is(TokenType.None, TokenType.Unknown) && Count <= 0 && Token.IsMatch("");
        public bool IsProcedure() => !Is(NodeType.None);
        public bool IsIndependent() => Is(NodeType.Program, NodeType.Rule);
        public bool IsDependent() => Is(NodeType.Compute);

        public Node Add(Node node)
        {
            if (node != null)
            {
                node.Parent = this;
                children.Add(node);
            }
            return this;
        }

        public bool Remove(Node node)
        {
            if (node == null) return true;
            var index = children.IndexOf(node);
            if (index < 0) return false;
            node.Parent = null;
            children.RemoveAt(index);
            return true;
        }

        public Node Insert(int index, Node node)
        {
            if (node != null)
            {
                node.Parent = this;
                children = children.Take(index).Concat(new[] { node }).Concat(children.Skip(index)).ToList();
            }
            return this;
        }

        public Node Trim(Func<Node, bool> selector)
        {
            return TrimStart(selector).TrimEnd(selector);
        }

        public Node TrimStart(Func<Node, bool> selector)
        {
            while (children.Count > 0)
            {
                if (selector(children[0]))
                {
                    children[0].Parent = null;
                    children.RemoveAt(0);
                }
                else
                {
                    children[0].TrimStart(selector);
                    return this;
                }
            }
            return this;
        }

        public Node TrimEnd(Func<Node, bool> selector)
        {
            while (children.Count > 0)
            {
                var l = children.Count - 1;
                if (selector(children[l]))
                {
                    children[l].Parent = null;
                    children.RemoveAt(l);
                }
                else
                {
                    children[l].TrimEnd(selector);
                    return this;
                }
            }
            return this;
        }

        public Node ForceChild(int index) => Child(index)??new Node();
        public Node Child(int index) => children.Count > index ? children[index] : null;
        public Node Ancestor(Func<Node, bool> aggregator) => Parent == null? null: (aggregator(Parent) ? Parent : Parent.Ancestor(aggregator));

        public IEnumerable<Node> Flat(Func<Node, bool> aggregator = null)
        {
            if (aggregator == null || aggregator(this)) yield return this;
            foreach (var child in children)
                foreach (var cc in child.Flat(aggregator))
                    yield return cc;
        }

        public Node AddRange(IEnumerable<Node> nodes)
        {
            foreach (var node in nodes) Add(node);
            return this;
        }

        public Node Clear()
        {
            children.Clear();
            return this;
        }

        public Node Revise(Func<Node, Node> visitor)
        {
            var n = visitor(this);
            Update(n.Token, n.Type, n.AccessType, n.Location, n.Children, n.Parent);
            var c = Count;
            for (int i = 0; i < c; i++)
                children[i].Revise(visitor);
            return this;
        }

        public override string ToString()
        {
            return string.Join(" ", new[] { Token.Value }.Concat(Children.Select(n => n.ToString())));
        }
    }
}
