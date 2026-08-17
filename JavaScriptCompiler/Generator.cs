// Converted from src/engine/DaRQ/JavaScript-Compiler/Generator.ts
using System;
using System.Collections.Generic;
using System.Linq;
using MiMFa.DaRQ.Compiler;
using MiMFa.DaRQ.Compiler.Core;
using MiMFa.DaRQ.Compiler.Generator;
using MiMFa.DaRQ.Compiler.Assembler;

namespace MiMFa.DaRQ.JavaScriptCompiler
{
    public class Generator : MiMFa.DaRQ.Compiler.Generator.Generator
    {
        protected int indention = 0;

        public IList<string> GenerateArray(IEnumerable<Node> nodes)
        {
            var walker = new MiMFa.DaRQ.Compiler.Assembler.NodeWalker(nodes.ToArray());
            return Generate(walker).ToList();
        }
        protected override string? GenerateRuleCode(Node node, MiMFa.DaRQ.Compiler.Assembler.NodeWalker walker)
        {
            if (node.Is(NodeType.Selector))
            {
                if (node.Is(NodeType.NormalSelector))
                {
                    var res = $"if({GenerateCode(node.ForceFirst, walker)}) {GenerateCodeLine(node.ForceChild(1), walker)}";
                    if (node.Count > 2) res += Compiler?.Options.MakeNewLine(indention) + "else " + GenerateCodeLine(node.ForceLast, walker);
                    return res;
                }
                else if (node.Is(NodeType.ShortSelector))
                {
                    return $"{GenerateCode(node.ForceFirst, walker)}? {GenerateCode(node.ForceChild(1), walker)} : {GenerateCode(node.ForceChild(2), walker)}";
                }
                else if (node.Is(NodeType.LongSelector))
                {
                    var parts = node.ForceLast.Children.Select(n =>
                    {
                        indention++;
                        string c;
                        if (n.Token.IsMatch("case"))
                        {
                            c = Compiler?.Options.MakeNewLine(indention) + $"case {GenerateCode(n.ForceFirst, walker)}:" + (n.Count > 1 ? Compiler?.Options.MakeNewLine(indention + 1) : "") + string.Join(Compiler?.Options.MakeNewLine(indention + 1) ?? "\n", GenerateArray(n.Children.Skip(1)));
                        }
                        else
                        {
                            c = Compiler?.Options.MakeNewLine(indention) + "default:" + (n.Count > 0 ? Compiler?.Options.MakeNewLine(indention + 1) : "") + string.Join(Compiler?.Options.MakeNewLine(indention + 1) ?? "\n", GenerateArray(n.Children));
                        }
                        indention--;
                        return c;
                    }).ToArray();
                    return $"switch({GenerateCode(node.ForceFirst, walker)}) {{" + string.Join("", parts) + (Compiler?.Options.MakeNewLine(indention) ?? "\n") + "}";
                }
                return $"if({GenerateCode(node.ForceFirst, walker)}) {GenerateCodeLine(node.ForceChild(1), walker)}" + (node.Count > 2 ? Compiler?.Options.MakeNewLine(indention) + "else " + GenerateCodeLine(node.ForceLast, walker) : "");
            }

            if (node.Is(NodeType.Iterator))
            {
                if (node.Is(NodeType.ConditionIterator))
                {
                    if (node.Is(NodeType.PostConditionIterator))
                        return $"do {GenerateCodeLine(node.ForceFirst, walker)}" + Compiler?.Options.MakeNewLine(indention) + $"while({string.Join(" ", GenerateArray(node.ForceLast.Children))})";
                    return $"while({string.Join(" ", GenerateArray(node.ForceFirst.Children))}) {GenerateCodeLine(node.ForceLast, walker)}" + Compiler?.Options.MakeNewLine(indention);
                }
                return $"for({string.Join(" ", GenerateArray(node.ForceFirst.Children))}) {GenerateCodeLine(node.ForceLast, walker)}" + Compiler?.Options.MakeNewLine(indention);
            }
            return string.Empty;
        }

        protected string? GenerateCodeLine(Node node, MiMFa.DaRQ.Compiler.Assembler.NodeWalker walker)
        {
            if (node.Is(NodeType.Block)) return GenerateCode(node, walker) ?? "";
            return GenerateCode(node, walker) ?? "";
        }

        protected override string? GenerateProcedureCode(Node node, MiMFa.DaRQ.Compiler.Assembler.NodeWalker walker)
        {
            return node.Token.Value + " " + string.Join(" ", GenerateArray(node.Children));
        }

        protected override string? GenerateComputeCode(Node node, MiMFa.DaRQ.Compiler.Assembler.NodeWalker walker)
        {
            return " " + node.Token.Value + " " + string.Join(" ", GenerateArray(node.Children));
        }

        protected override string? GeneratePlainCode(Node node, MiMFa.DaRQ.Compiler.Assembler.NodeWalker walker)
        {
            if (node.Count > 0)
                return $"{node.Token.Value}{string.Concat(GenerateArray(node.Children))}";
            return node.ToString();
        }

        protected override string? GenerateDefineCode(Node node, MiMFa.DaRQ.Compiler.Assembler.NodeWalker walker)
        {
            // function declarations and arrow functions
            if (node.Token.Is(TokenType.FunctionKeyword))
            {
                if (!string.IsNullOrEmpty(node.Token.Value))
                    return $"function {node.Token.Value}({string.Join(", ", GenerateArray(node.First?.Children ?? new Node[0]))}) {string.Join("", GenerateArray(node.Children.Skip(1)))}{Compiler?.Options.MakeNewLine(indention)}";
                else
                    return $"({string.Join(", ", GenerateArray(node.First?.Children ?? new Node[0]))}) => {string.Join("", GenerateArray(node.Children.Skip(1)))}";
            }

            // arrow operator node
            if (node.Token.IsMatch("=>") || node.Token.Value == "=>")
            {
                var left = node.First != null ? GenerateCode(node.First, walker) : "";
                var right = string.Join("", GenerateArray(node.Children));
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

            return $"{node.Token.Value} {string.Join(" ", GenerateArray(node.Children ?? new Node[0]))}";
        }

        protected override string? GenerateCallCode(Node node, MiMFa.DaRQ.Compiler.Assembler.NodeWalker walker)
        {
            if (node.Token.Is(TokenType.FunctionKeyword))
                return $"{node.Token.Value}({string.Join(", ", GenerateArray(node.Children))})";
            if (node.Count > 0)
            {
                // optional chaining handling
                if (node.Token.Is(TokenType.NamespaceKeyword) || node.Token.Value.Contains("?."))
                    return $"{node.Token.Value}{(node.Token.Value.Contains("?.") ? "" : ".")}{string.Join(".", GenerateArray(node.Children))}";

                // spread handling in calls
                if (node.Token.IsMatch("..."))
                    return $"...{string.Join(", ", GenerateArray(node.Children))}";

                return $"{node.Token.Value} {string.Join(" ", GenerateArray(node.Children))}";
            }
            else return node.ToString();
        }

            
        protected override string? GenerateBlockCode(Node node, MiMFa.DaRQ.Compiler.Assembler.NodeWalker walker)
            {
                if (node.Token.Is(TokenType.ObjectData))
                {
                    indention++;
                    var inner = string.Join(Compiler?.Options.MakeNewLine(indention) ?? "\n", GenerateArray(node.Children));
                    indention--;
                    return $"{{{Compiler?.Options.MakeNewLine(++indention)}" + inner + $"{Compiler?.Options.MakeNewLine(--indention)}}}";
                }
                if (node.Token.Is(TokenType.ArrayData))
                {
                    indention++;
                    var parameters = string.Join(", ", GenerateArray(node.Children));
                    indention--;
                    return $"[{parameters}]";
                }
                if (node.Token.Is(TokenType.Scope))
                {
                    if (node.Token.IsMatch("{"))
                    {
                        indention++;
                        var body = string.Join(Compiler?.Options.MakeNewLine(indention) ?? "\n", GenerateArray(node.Children));
                        indention--;
                        return $"{{{Compiler?.Options.MakeNewLine(indention)}" + body + $"{Compiler?.Options.MakeNewLine(indention)}}}";
                    }
                    else if (node.Token.IsMatch("["))
                    {
                        indention++;
                        var parameters = string.Join(", ", GenerateArray(node.Children));
                        indention--;
                        return $"[{parameters}]";
                    }
                    else if (node.Token.IsMatch("("))
                    {
                        indention++;
                        var parameters = string.Join(" ", GenerateArray(node.Children));
                        indention--;
                        return $"({parameters})";
                    }
                }
                return string.Join(" ", GenerateArray(node.Children));
            }

        protected override string? GenerateHelperCode(Node node, MiMFa.DaRQ.Compiler.Assembler.NodeWalker walker)
        {
            if (node.Count > 0)
                return $"{node.Token.Value} {string.Join(" ", GenerateArray(node.Children)).TrimEnd()}".Replace("\n", Compiler?.Options.MakeNewLine(indention) ?? "\n");
            else return node.ToString().Replace("\n", Compiler?.Options.MakeNewLine(indention) ?? "\n");
        }

        protected override string? GenerateProgramCode(Node node, MiMFa.DaRQ.Compiler.Assembler.NodeWalker walker)
        {
            if (Compiler != null) return Transform(node, Compiler) as string;
            return null;
        }

        protected override string? GenerateUnknownCode(Node node, MiMFa.DaRQ.Compiler.Assembler.NodeWalker walker)
        {
            return node.Token.Value;
        }
    }
}
