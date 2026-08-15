// Converted from src/engine/DaRQ/JavaScript-Compiler/Tokenizer.ts
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DaRQ.Compiler.Core;
using DaRQ.Compiler.Tokenizer;

namespace DaRQ.JavaScriptCompiler
{
    public class Tokenizer : DaRQ.Compiler.Tokenizer.Tokenizer
    {
        protected override Token TokenizeCode(CodeWalker walker)
        {
            walker.MoveToProcedure();
            var location = walker.Location;
            var current = walker.Current;
            if (current != default(char))
            {
                var next = walker.Peek(1);
                var word = string.Concat(walker.PeekUntil(ch => Regex.IsMatch(ch.ToString(), "[^A-Z0-9_$]", RegexOptions.IgnoreCase)).ToArray());

                switch (word)
                {
                    case "true":
                    case "false":
                        walker.Move(word.Length);
                        walker.MoveToProcedure();
                        return new Token(TokenType.BooleanData, word, location);
                    case "null":
                        walker.Move(word.Length);
                        walker.MoveToProcedure();
                        return new Token(TokenType.NullData, word, location);
                    case "undefined":
                        walker.Move(word.Length);
                        walker.MoveToProcedure();
                        return new Token(TokenType.UndefinedData, word, location);

                    case "if": case "else": case "switch": case "case": case "default":
                    case "for": case "while": case "do": case "break": case "continue":
                    case "try": case "catch": case "finally": case "throw":
                    case "return": case "yield":
                    case "import": case "export":
                    case "void":
                    case "const": case "let": case "var":
                    case "with": case "debugger":
                        walker.Move(word.Length);
                        walker.MoveToProcedure();
                        return new Token(TokenType.Statement, word, location);

                    case "delete":
                    case "typeof": case "instanceof":
                    case "await": case "async": case "new":
                        walker.Move(word.Length);
                        walker.MoveToProcedure();
                        return new Token(TokenType.Facilitator, word, location);

                    case "super": case "this":
                        walker.Move(word.Length);
                        walker.MoveToProcedure();
                        return new Token(TokenType.IdentifierKeyword, word, location);

                    case "private": case "internal": case "protected": case "public": case "static":
                        walker.Move(word.Length);
                        walker.MoveToProcedure();
                        return new Token(TokenType.Access, word, location);

                    case "of": case "in":
                    case "implements": case "extends":
                    case "interface": case "class": case "enum": case "package":
                    case "get": case "set": case "function":
                        walker.Move(word.Length);
                        walker.MoveToProcedure();
                        return new Token(TokenType.Structure, word, location);
                }

                if (current == '/')
                {
                    if (next == '*') return TokenizeCommentBlock(walker, location);
                    else if (next == '/') return TokenizeComment(walker, location);
                    else return TokenizeRegExPath(walker, location);
                }

                if (Regex.IsMatch(current.ToString(), "[A-Z_$]", RegexOptions.IgnoreCase))
                    return TokenizeKeyword(walker, location);

                if (Regex.IsMatch(current.ToString(), "[0-9]"))
                    return TokenizeNumber(walker, location);

                if (current == '"' || current == '\'' || current == '`')
                    return TokenizeString(walker, location);

                if (Regex.IsMatch(current.ToString(), "[^\\w\\d\\s_$]", RegexOptions.IgnoreCase))
                    return TokenizeOperator(walker, location);
            }

            return new Token(TokenType.Unknown, walker.Walk().ToString(), location);
        }

        protected Token TokenizeKeyword(CodeWalker walker, Location location)
        {
            var chars = walker.WalkWhile(ch => Regex.IsMatch(ch.ToString(), "[A-Za-z0-9_$]")).ToArray();
            var value = string.Concat(chars);
            var type = TokenType.Keyword;
            walker.MoveToProcedure();
            var sign = string.Concat(walker.PeekCount(2).ToArray());
            if (sign == "++" || sign == "--")
            {
                value += sign;
                type = TokenType.IdentifierKeyword;
                walker.Move(2);
            }
            return new Token(type, value, location);
        }

        protected Token TokenizeNumber(CodeWalker walker, Location location)
        {
            var value = string.Concat(walker.WalkWhile(ch => Regex.IsMatch(ch.ToString(), "[0-9._]")).ToArray());
            return new Token(TokenType.NumberData, value, location);
        }

        protected Token TokenizeRegExPath(CodeWalker walker, Location location)
        {
            walker.Walk(); // consume '/'
            var value = string.Concat(walker.WalkUntil(ch => ch == '/' && walker.Peek(-1) != (this.Compiler?.Options?.Escape ?? "\\")[0]).ToArray());
            walker.Walk(); // consume closing '/'
            value = "/" + value + "/" + string.Concat(walker.WalkWhile(ch => Regex.IsMatch(ch.ToString(), "[gimsuy]", RegexOptions.IgnoreCase)).ToArray());
            return new Token(TokenType.RegExPathData, value, location);
        }

        protected Token TokenizeString(CodeWalker walker, Location location)
        {
            var quote = walker.Walk();
            var value = "";
            var escaped = false;
            while (!walker.IsEnded)
            {
                var ch = walker.Walk();
                if (!escaped && ch == quote) break;
                if (ch == (this.Compiler?.Options?.Escape ?? "\\")[0] && !escaped)
                {
                    escaped = true;
                    continue;
                }
                if (ch != default(char)) value += ch;
                escaped = false;
            }
            return new Token(quote == '`' ? TokenType.TemplateStringData : TokenType.StringData, value, location);
        }

        protected Token TokenizeOperator(CodeWalker walker, Location location)
        {
            var sign = string.Concat(walker.WalkUntil(ch => Regex.IsMatch(ch.ToString(), "[\\w\\d\\s_$]", RegexOptions.IgnoreCase)).ToArray());
            while (true)
            {
                switch (sign)
                {
                    case "*": case "/": case "**": case "%": case "+": case "-": case "~": case "^": case "=": case "<<": case ">>": case ">>>":
                    case "==": case "!=": case "===": case "!==": case "<": case "<=": case ">": case ">=": case "!": case "&&": case "||": case "&": case "|": case "??":
                    case "+=": case "-=": case "*=": case "/=": case "**=": case "^=": case "%=":
                    case "&&=": case "&=": case "||=": case "|=": case "??=":
                        return new Token(TokenType.OperatorSymbol, sign, location);
                    case "++": case "--":
                        return new Token(TokenType.IdentifierKeyword, sign + TokenizeCode(walker).Value, location);
                    case "...":
                        return new Token(TokenType.Symbol, sign + TokenizeCode(walker).Value, location);
                    case "=>":
                        return new Token(TokenType.ConcatenatorSymbol, sign, location);
                    case ".":
                        return new Token(TokenType.ConcatenatorSymbol, sign, location);
                    case ",": case ";": case ":":
                        return new Token(TokenType.SeparatorSymbol, sign, location);
                    case "{": case "[": case "(": case "<":
                        return new Token(TokenType.StartScope, sign, location);
                    case "}": case "]": case ")": case ">":
                        return new Token(TokenType.EndScope, sign, location);
                    default:
                        if (sign.Length <= 1)
                            return new Token(TokenType.Symbol, sign, location);
                        break;
                }
                if (sign.Length > 1)
                {
                    walker.Move(-1);
                    sign = sign.Substring(0, sign.Length - 1);
                    continue;
                }
            }
        }

        protected Token TokenizeComment(CodeWalker walker, Location location)
        {
            walker.Move(2);
            var value = string.Concat(walker.WalkUntil(ch => ch == '\n').ToArray());
            return new Token(TokenType.Comment, value, location);
        }

        protected Token TokenizeCommentBlock(CodeWalker walker, Location location)
        {
            walker.Move(2);
            var value = string.Concat(walker.WalkUntil(ch => ch == '*' && walker.Peek(1) == '/').ToArray());
            walker.Move(2);
            return new Token(TokenType.Comment, value, location);
        }
    }
}
