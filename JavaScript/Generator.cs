// Converted from src/engine/DaRQ/JavaScript-Compiler/Generator.ts
using System;
using System.Collections.Generic;
using System.Linq;
using MiMFa.Compiler;
using MiMFa.Compiler.Core;
using MiMFa.Compiler.Generator;
using MiMFa.Compiler.Assembler;
using MiMFa.Compiler.Model;
using MiMFa.Compiler.Walker;

namespace MiMFa.Compiler.JavaScript
{
    public class Generator : MiMFa.Compiler.Generator.Generator
    {
        protected virtual IList<string> GenerateArray(IEnumerable<Node> nodes)
        {
            var walker = new NodeWalker(nodes.ToArray());
            return Generate(walker).ToList();
        }
        protected virtual string GenerateArrayCode(IEnumerable<Node> nodes, string separator = " ")
        {
            return string.Join(separator??" ", GenerateArray(nodes));
        }
        protected virtual string GenerateCodeLine(Node node, NodeWalker walker)
        {
            if (node.Is(NodeType.Block) && node.Token.IsMatch("{")) return GenerateCode(node, walker) ?? "";
            var code = GenerateCode(node, walker) ?? "";
            return string.IsNullOrEmpty(code)?"": System.Text.RegularExpressions.Regex.IsMatch(code, ";\\s*$")? code:$"{code};";
        }

        protected override string GenerateRuleCode(Node node, NodeWalker walker)
        {
            if (node.Is(NodeType.Selector))
            {
                if (node.Is(NodeType.NormalSelector))
                {
                    var res = $"if({GenerateArrayCode(node.ForceFirst.Children)}) {GenerateCodeLine(node.ForceChild(1), walker)}";
                    if (node.Count > 2) res += Compiler?.Options.MakeNewLine(Indention) + "else " + GenerateCodeLine(node.ForceLast, walker);
                    return res;
                }
                else if (node.Is(NodeType.ShortSelector) && node.Count>2)
                    return $"({GenerateCode(node.ForceFirst, walker)}? {GenerateCode(node.ForceChild(1), walker)} : {GenerateCode(node.ForceChild(2), walker)})";
                else if (node.Is(NodeType.LongSelector))
                {
                    var parts = node.ForceLast.Children.Select(n =>
                    {
                        Indention++;
                        string c;
                        if (n.Token.IsMatch("case"))
                            c = Compiler?.Options.MakeNewLine(Indention) + $"case {GenerateCode(n.ForceFirst, walker)}:" + (n.Count > 1 ? Compiler?.Options.MakeNewLine(Indention + 1) : "") +  GenerateArrayCode(n.Children.Skip(1), Compiler?.Options.MakeNewLine(Indention + 1) ?? "\n");
                        else
                            c = Compiler?.Options.MakeNewLine(Indention) + "default:" + (n.Count > 0 ? Compiler?.Options.MakeNewLine(Indention + 1) : "") + GenerateArrayCode(n.Children, Compiler?.Options.MakeNewLine(Indention + 1) ?? "\n");
                        Indention--;
                        return c;
                    }).ToArray();
                    return $"switch({GenerateArrayCode(node.ForceFirst.Children)}) {{" + string.Join("", parts) + (Compiler?.Options.MakeNewLine(Indention) ?? "\n") + "}";
                }
                return $"if({GenerateArrayCode(node.ForceFirst.Children)}) {GenerateCodeLine(node.ForceChild(1), walker)}" + (node.Count > 2 ? Compiler?.Options.MakeNewLine(Indention) + "else " + GenerateCodeLine(node.ForceLast, walker) : "");
            }

            if (node.Is(NodeType.Iterator))
            {
                if (node.Is(NodeType.ConditionIterator))
                {
                    if (node.Is(NodeType.PostConditionIterator))
                        return $"do {GenerateCodeLine(node.ForceFirst, walker)}" + Compiler?.Options.MakeNewLine(Indention) + $"while({GenerateArrayCode(node.ForceLast.Children)})";
                    return $"while({GenerateArrayCode(node.ForceFirst.Children)}) {GenerateCodeLine(node.ForceLast, walker)}" + Compiler?.Options.MakeNewLine(Indention);
                }
                return $"for({GenerateArrayCode(node.ForceFirst.Children)}) {GenerateCodeLine(node.ForceLast, walker)}" + Compiler?.Options.MakeNewLine(Indention);
            }
            return string.Empty;
        }

        protected override string GenerateProcedureCode(Node node, NodeWalker walker)
        {
            return node.Token.Value + " " + (GenerateArrayCode(node.Children));
        }

        protected override string GenerateComputeCode(Node node, NodeWalker walker)
        {
            return " " + node.Token.Value + " " + (GenerateArrayCode(node.Children));
        }

        protected override string GeneratePlainCode(Node node, NodeWalker walker)
        {
            if (node.Count > 0)
                return $"{node.Token.Value}{GenerateArrayCode(node.Children, "")}";
            return node.ToString();
        }

        protected override string GenerateDefineCode(Node node, NodeWalker walker)
        {
            // function declarations and arrow functions
            if (node.Token.Is(TokenType.FunctionKeyword))
            {
                if (!string.IsNullOrEmpty(node.Token.Value))
                    return $"function {node.Token.Value}({GenerateArrayCode(node.First?.Children ?? new Node[0],", ")}) {GenerateArrayCode(node.Children.Skip(1),"")}{Compiler?.Options.MakeNewLine(Indention)}";
                else
                    return $"({GenerateArrayCode(node.First?.Children ?? new Node[0],", ")}) => {GenerateArrayCode(node.Children.Skip(1),"")}";
            }

            // arrow operator node
            if (node.Token.IsMatch("=>") || node.Token.Value == "=>")
            {
                var left = node.First != null ? GenerateCode(node.First, walker) : "";
                var right = GenerateArrayCode(node.Children,"");
                return left + " => " + right;
            }

            // class declaration
            if (node.Token.IsMatch("class") || node.Token.Value == "class")
            {
                string name = node.Children != null && node.Children.Count > 0 ? node.Children[0].Token?.Value ?? "" : "";
                string extendsPart = "";
                for (int i = 0; node.Children != null && i < node.Children.Count; i++)
                {
                    var c = node.Children[i];
                    if (c.Token != null && c.Token.IsMatch("extends") && i + 1 < node.Children.Count)
                    {
                        extendsPart = " extends " + GenerateCode(node.Children[i + 1], walker);
                        break;
                    }
                }
                var body = node.Children != null && node.Children.Count > 0 ? GenerateArray(node.Children).LastOrDefault() ?? "{}" : "{}";
                return $"class {name}{extendsPart} {body}";
            }

            if(string.IsNullOrEmpty(node.Token.Value)) return $"{GenerateArrayCode(node.Children ?? new Node[0])}";
            else return $"{node.Token.Value} {GenerateArrayCode(node.Children ?? new Node[0])}";
        }

        protected override string GenerateCallCode(Node node, NodeWalker walker)
        {
            if (node.Token.Is(TokenType.FunctionKeyword))
                return $"{node.Token.Value}({GenerateArrayCode(node.Children,", ")})";
            if (node.Count > 0)
            {
                // optional chaining handling
                if (node.Token.Is(TokenType.NamespaceKeyword))
                    return $"{node.Token.Value}{GenerateArrayCode(node.Children,"")}";

                // spread handling in calls
                if (node.Token.IsMatch("..."))
                    return $"...{GenerateArrayCode(node.Children, ", ")}";

                return $"{node.Token.Value} {GenerateArrayCode(node.Children)}";
            }
            else return node.ToString();
        }

        protected override string GenerateBlockCode(Node node, NodeWalker walker)
        {
            if (node.Token.Is(TokenType.ObjectData))
            {
                Indention++;
                var inner = GenerateArrayCode(node.Children, Compiler?.Options.MakeNewLine(Indention) ?? "\n");
                Indention--;
                return $"{{{Compiler?.Options.MakeNewLine(++Indention)}" + inner + $"{Compiler?.Options.MakeNewLine(--Indention)}}}";
            }
            if (node.Token.Is(TokenType.ArrayData))
            {
                Indention++;
                var parameters = GenerateArrayCode(node.Children, ", ");
                Indention--;
                return $"[{parameters}]";
            }
            if (node.Token.Is(TokenType.Scope))
            {
                if (node.Token.IsMatch("{"))
                {
                    Indention++;
                    var body = GenerateArrayCode(node.Children, Compiler?.Options.MakeNewLine(Indention) ?? "\n");
                    Indention--;
                    return $"{{{Compiler?.Options.MakeNewLine(Indention + 1)}" + body + $"{Compiler?.Options.MakeNewLine(Indention)}}}";
                }
                else if (node.Token.IsMatch("["))
                {
                    Indention++;
                    var parameters = GenerateArrayCode(node.Children, ", ");
                    Indention--;
                    return $"[{parameters}]";
                }
                else if (node.Token.IsMatch("("))
                {
                    Indention++;
                    var parameters = (GenerateArrayCode(node.Children));
                    Indention--;
                    return $"({parameters})";
                }
            }
            return GenerateArrayCode(node.Children, Compiler?.Options.MakeNewLine(Indention));
        }

        protected override string GenerateHelperCode(Node node, NodeWalker walker)
        {
            if (node.Count > 0)
                return $"{node.Token.Value} {GenerateArrayCode(node.Children,"\n").TrimEnd()}";
            else return System.Text.RegularExpressions.Regex.Replace(node.ToString(),"\\s*\\r*\\n+\\r*\\s*$", Compiler?.Options.MakeNewLine(Indention)??"\n");
        }

        protected override string GenerateProgramCode(Node node, NodeWalker walker)
        {
            if (Compiler != null) return Transform(node, Compiler) as string;
            return null;
        }

        protected override string GenerateUnknownCode(Node node, NodeWalker walker)
        {
            return node.Token.Value;
        }
    }
}
