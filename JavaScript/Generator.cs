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
        protected virtual string GenerateArrayCode(IEnumerable<Node> nodes, string separator = "")
        {
            return string.Join(separator??" ", GenerateArray(nodes));
        }
        protected virtual string GenerateCodeLine(Node node, NodeWalker walker)
        {
            if (node.Is(NodeType.BlockStructure) && node.Token.IsMatch("{")) return GenerateCode(node, walker) ?? "";
            var code = GenerateCode(node, walker) ?? "";
            return string.IsNullOrEmpty(code)?"": System.Text.RegularExpressions.Regex.IsMatch(code, "[;\\}]\\s*$")? code:$"{code};";
        }

        protected override string GenerateProgramCode(Node node, NodeWalker walker)
        {
            if (Compiler != null) return
                    Transform(node, Compiler) as string +
                    Compiler?.Options.MakeNewLine(Indention) +
                    Compiler?.Options.MakeNewLine(Indention);
            return null;
        }
        protected override string GenerateStructureCode(Node node, NodeWalker walker)
        {
            if (node.Is(NodeType.BlockStructure))
            {

                if (node.Token.Is(TokenType.ObjectData))
                {
                    Indention++;
                    var inner = GenerateArrayCode(node.Children, Compiler?.Options.MakeNewLine(Indention) ?? "");
                    Indention--;
                    return node.Token.Value + Compiler?.Options.MakeNewLine(++Indention) + inner.Trim() + Compiler?.Options.MakeNewLine(--Indention) + "}";
                }
                if (node.Token.Is(TokenType.ArrayData))
                {
                    Indention++;
                    var parameters = GenerateArrayCode(node.Children);
                    Indention--;
                    return $"{node.Token.Value}{parameters}]";
                }
                if (node.Token.Is(TokenType.Scope))
                {
                    if (node.Token.IsMatch("{"))
                    {
                        Indention++;
                        var body = GenerateArrayCode(node.Children, Compiler?.Options.MakeNewLine(Indention) ?? "");
                        Indention--;
                        return $"{node.Token.Value}{Compiler?.Options.MakeNewLine(++Indention)}{body.Trim()}{Compiler?.Options.MakeNewLine(--Indention)}}}";
                    }
                    else if (node.Token.IsMatch("["))
                    {
                        Indention++;
                        var parameters = GenerateArrayCode(node.Children);
                        Indention--;
                        return $"{node.Token.Value}{parameters}]";
                    }
                    else if (node.Token.IsMatch("("))
                    {
                        Indention++;
                        var parameters = GenerateArrayCode(node.Children);
                        Indention--;
                        return $"{node.Token.Value}{parameters})";
                    }
                }
                return $"{node.Token.Value}{GenerateArrayCode(node.Children, Compiler?.Options.MakeNewLine(Indention + 1))}".TrimEnd();
            }

            string prefix = "", suffix = "";
            if (node.Is(NodeType.Region))
                prefix = suffix = Compiler?.Options.MakeNewLine(Indention);
            //else if (node.Is(NodeType.Line))
            //    prefix = Compiler?.Options.MakeNewLine(Indention);

            if (node.Is(NodeType.DefineStructure))
            {
                if (node.Token.Is(TokenType.FunctionKeyword))
                    return $"{prefix}{node.Token.Value} {GenerateArrayCode(node.Children, " ")}{suffix}";
                if (string.IsNullOrEmpty(node.Token.Value))
                    return $"{prefix}{GenerateArrayCode(node.Children ?? new Node[0], " ")}{suffix}";
                else return $"{prefix}{node.Token.Value} {GenerateArrayCode(node.Children ?? new Node[0], " ")}{suffix}";
            }

            if (node.Is(NodeType.CallStructure))
            {
                if (node.Token.Is(TokenType.FunctionKeyword))
                    return $"{prefix}{node.Token.Value}{GenerateArrayCode(node.Children)}{suffix}";
                if (node.Token.Is(TokenType.NamespaceKeyword))
                    return $"{prefix}{node.Token.Value}{GenerateArrayCode(node.Children)}{suffix}";
                if(node.ForceFirst.Is(TokenType.DelimiterSymbol, TokenType.TerminatorSymbol, TokenType.End, TokenType.Suffix, TokenType.ConcatenatorSymbol))
                    return $"{prefix}{node.Token.Value}{GenerateArrayCode(node.Children)}{suffix}".TrimEnd();
                else return $"{prefix}{node.Token.Value} {GenerateArrayCode(node.Children)}{suffix}".TrimEnd();
            }
            return $"{prefix}{node.Token.Value} {GenerateArrayCode(node.Children, " ")}{suffix}".TrimEnd();

            //if (node.Is(NodeType.SelectorStructure))
            //{
            //    if (node.Is(NodeType.NormalSelectorStructure))
            //    {
            //        var res = $"if({GenerateArrayCode(node.ForceFirst.Children)}) {GenerateCodeLine(node.ForceChild(1), walker)}";
            //        if (node.Count > 2) res += Compiler?.Options.MakeNewLine(Indention) + "else " + GenerateCodeLine(node.ForceLast, walker);
            //        return res;
            //    }
            //    else if (node.Is(NodeType.ShortSelectorStructure) && node.Count > 2)
            //        return $"{GenerateCode(node.ForceFirst, walker)}? {GenerateCode(node.ForceChild(1), walker)} : {GenerateCode(node.ForceChild(2), walker)}";
            //    else if (node.Is(NodeType.LongSelectorStructure))
            //    {
            //        var parts = node.ForceLast.Children.Select(n =>
            //        {
            //            Indention++;
            //            string c;
            //            if (n.Token.IsMatch("case"))
            //                c = Compiler?.Options.MakeNewLine(Indention) + $"case {GenerateCode(n.ForceFirst, walker)}:" + (n.Count > 1 ? Compiler?.Options.MakeNewLine(Indention + 1) : "") + GenerateArrayCode(n.Children.Skip(1), Compiler?.Options.MakeNewLine(Indention + 1) ?? "\n");
            //            else
            //                c = Compiler?.Options.MakeNewLine(Indention) + "default:" + (n.Count > 0 ? Compiler?.Options.MakeNewLine(Indention + 1) : "") + GenerateArrayCode(n.Children, Compiler?.Options.MakeNewLine(Indention + 1) ?? "\n");
            //            Indention--;
            //            return c;
            //        }).ToArray();
            //        return $"switch({GenerateArrayCode(node.ForceFirst.Children)}) {{" + string.Join("", parts) + (Compiler?.Options.MakeNewLine(Indention) ?? "\n") + "}";
            //    }
            //    return $"if({GenerateArrayCode(node.ForceFirst.Children)}) {GenerateCodeLine(node.ForceChild(1), walker)}" + (node.Count > 2 ? Compiler?.Options.MakeNewLine(Indention) + "else " + GenerateCodeLine(node.ForceLast, walker) : "");
            //}

            //if (node.Is(NodeType.IteratorStructure))
            //{
            //    if (node.Is(NodeType.ConditionIteratorStructure))
            //    {
            //        if (node.Is(NodeType.PostConditionIteratorStructure))
            //            return $"do {GenerateCodeLine(node.ForceFirst, walker)}" + Compiler?.Options.MakeNewLine(Indention) + $"while({GenerateArrayCode(node.ForceLast.Children)})";
            //        return $"while({GenerateArrayCode(node.ForceFirst.Children)}) {GenerateCodeLine(node.ForceLast, walker)}" + Compiler?.Options.MakeNewLine(Indention);
            //    }
            //    return $"for({GenerateArrayCode(node.ForceFirst.Children)}) {GenerateCodeLine(node.ForceLast, walker)}" + Compiler?.Options.MakeNewLine(Indention);
            //}
            //return string.Empty;
        }
        protected override string GenerateRegionCode(Node node, NodeWalker walker)
        {
            if (node.Count > 0)
                return $"{Compiler.Options.MakeNewLine(Indention)}{node.Token.Value} {GenerateArrayCode(node.Children).TrimEnd()}{Compiler.Options.MakeNewLine(Indention)}";
            else return $"{Compiler.Options.MakeNewLine(Indention)}{node.Token.Value}{Compiler.Options.MakeNewLine(Indention)}";
        }
        protected override string GenerateLineCode(Node node, NodeWalker walker)
        {
            if (node.Count > 0)
                return $"{node.Token.Value} {GenerateArrayCode(node.Children).TrimEnd()}{Compiler.Options.MakeNewLine(Indention)}";
            else return $"{node.Token.Value}{Compiler.Options.MakeNewLine(Indention)}";
        }
        protected override string GenerateChunkCode(Node node, NodeWalker walker)
        {
            if (node.Count > 0)
                return $"{node.Token.Value}{GenerateArrayCode(node.Children)}";
            return node.ToString();
        }
        protected override string GenerateIndependCode(Node node, NodeWalker walker)
        {
            if (node.Count > 0)
                return $"{node.Token.Value}{Compiler?.Options.MakeNewLine(Indention)}{GenerateArrayCode(node.Children).TrimEnd()}";
            else return $"{node.Token.Value}{Compiler?.Options.MakeNewLine(Indention)}";
        }
        protected override string GenerateDependCode(Node node, NodeWalker walker)
        {
            if(node.Is(TokenType.ConcatenatorSymbol)) return node.Token.Value + (GenerateArrayCode(node.Children));
            if(node.Is(TokenType.DelimiterSymbol, TokenType.TerminatorSymbol)) return node.Token.Value + " " + (GenerateArrayCode(node.Children));
            else return " " + node.Token.Value + " " + GenerateArrayCode(node.Children);
        }
        protected override string GenerateAppendCode(Node node, NodeWalker walker)
        {
            if(node.Is(TokenType.TerminatorSymbol)) return node.Token.Value + GenerateArrayCode(node.Children) + Compiler?.Options.MakeNewLine(Indention);
            return " " + node.Token.Value + "" + GenerateArrayCode(node.Children);
        }
        protected override string GeneratePrependCode(Node node, NodeWalker walker)
        {
            return node.Token.Value + " " + GenerateArrayCode(node.Children);
        }
        protected override string GenerateUnknownCode(Node node, NodeWalker walker)
        {
            return node.ToString();
        }
    }
}
