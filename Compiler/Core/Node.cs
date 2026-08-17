// Converted from src/engine/DaRQ/Compiler/Core/Node.ts
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiMFa.DaRQ.Compiler.Core
{
    public class Node
    {
        public Token Token { get; set; }
        public NodeType Type { get; set; }

        protected Node? parent = null;
        public Node? Parent => parent;

        protected List<Node> children = new List<Node>();
        public IList<Node> Children
        {
            get => children;
            set
            {
                children.Clear();
                foreach (var v in value) children.Add(v);
            }
        }

        public AccessType AccessType { get; set; }

        public int Count => children.Count;
        public Node ForceFirst
        {
            get => children[0];
            set
            {
                if (children.Count > 0) children[0] = value;
                else Add(value);
            }
        }
        public Node ForceLast
        {
            get => children[children.Count - 1];
            set
            {
                if (children.Count > 0) children[children.Count - 1] = value;
                else Add(value);
            }
        }
        public Node? First => children.Count > 0 ? children[0] : null;
        public Node? Last => children.Count > 0 ? children[children.Count - 1] : null;

        public Node FirstLeaf => First != null ? First.FirstLeaf : this;
        public Node LastLeaf => Last != null ? Last.LastLeaf : this;

        public Node(Token? token = null, NodeType? type = null, AccessType accessType = AccessType.Unknown, IEnumerable<Node?>? children = null, Node? parent = null)
        {
            Token = token ?? new Token();
            Type = type ?? (token != null ? NodeType.Unknown : NodeType.None);
            this.parent = parent;
            Children = (from v in children where v != null select v).ToList() ?? new List<Node>();
            AccessType = accessType;
        }

        public Node Update(Node node)
        {
            Token = node.Token ?? Token;
            Type = node.Type != 0 ? node.Type : Type;
            parent = node.parent ?? parent;
            Children = node.Children ?? Children;
            AccessType = node.AccessType != 0 ? node.AccessType : AccessType;
            return this;
        }

        public Node Clone(Node? node = null)
        {
            var n = node ?? this;
            return new Node(n.Token ?? Token, n.Type != 0 ? n.Type : Type, n.AccessType != 0 ? n.AccessType : AccessType, n.Children?.Select(c => c.Clone()).ToList(), n.parent ?? parent);
        }

        public bool Is(params NodeType[] nodeTypes)
        {
            foreach (var nt in nodeTypes)
                if (((int)Type & (int)nt) == (int)nt) return true;
            return false;
        }

        public bool IsProcedure() => !Is(NodeType.None);
        public bool IsIndependent() => Is(NodeType.Program, NodeType.Rule);
        public bool IsDependent() => Is(NodeType.Compute);

        public Node Add(Node node)
        {
            if (node != null)
            {
                node.parent = this;
                children.Add(node);
            }
            return this;
        }

        public bool Remove(Node node)
        {
            if (node == null) return true;
            var index = children.IndexOf(node);
            if (index < 0) return false;
            node.parent = null;
            children.RemoveAt(index);
            return true;
        }

        public Node Insert(int index, Node node)
        {
            if (node != null)
            {
                node.parent = this;
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
                    children[0].parent = null;
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
                    children[l].parent = null;
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

        public Node TrimSeparators()
        {
            return Trim(n => n.Count <= 0 && n.Token.Is(TokenType.SeparatorSymbol));
        }

        public Node ForceChild(int index) => children[index];
        public Node? Child(int index) => children.Count > index ? children[index] : null;

        public IEnumerable<Node> Flat(Func<Node, bool>? aggregator = null)
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
            Update(visitor(this));
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
