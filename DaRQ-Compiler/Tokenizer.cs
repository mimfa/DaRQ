// Converted from src/engine/DaRQ/DaRQ-Compiler/Tokenizer.ts
using System;
using System.Linq;
using System.Text.RegularExpressions;
using DaRQ.Compiler.Tokenizer;
using DaRQ.Compiler.Tokenizer;
using DaRQ.Compiler.Tokenizer;
using DaRQ.Compiler.Tokenizer;
using DaRQ.Compiler.Core;

namespace DaRQ.DaRQ_Compiler
{
    public class Tokenizer : DaRQ.JavaScriptCompiler.Tokenizer
    {
        public dynamic DaRQCompiler { get; set; }

        public Token[] Transform(string input, dynamic compiler)
        {
            return base.Transform(input, compiler) as Token[];
        }

        protected TokenType? SwitchToken { get; set; }
        protected bool SwitchNamespace { get; set; } = false;

        protected override Token TokenizeCode(CodeWalker walker)
        {
            walker.MoveToProcedure();
            if (this.SwitchToken != null)
            {
                var type = this.SwitchToken.Value;
                this.SwitchToken = null;
                return new Token(type, walker.WalkProcedure(), walker.Location);
            }
            var switchNameSpace = this.SwitchNamespace;
            if (this.SwitchNamespace) this.SwitchNamespace = false;
            if (walker.Current == '.' && !walker.PeekProcedure().StartsWith("...")) this.SwitchNamespace = true;
            else if (!switchNameSpace && walker.Current != default(char))
            {
                var location = walker.Location;
                var position = walker.Position;
                var word = string.Concat(walker.PeekUntil(ch => Regex.IsMatch(ch.ToString(), "[^A-Z0-9_$]", RegexOptions.IgnoreCase)).ToArray());

                switch (word.ToLower())
                {
                    case "use":
                        walker.Move(word.Length);
                        walker.MoveToProcedure();
                        var src = TokenizeCode(walker)?.Value;
                        if (!string.IsNullOrEmpty(src))
                        {
                            UsePath(src);
                            walker.MoveToProcedure();
                            if (walker.Walk() != ';') walker.Move(-1);
                            return new Token(TokenType.Comment, "Contents from \"" + src + "\"", location);
                        }
                        else walker.Reset(position);
                        break;
                    case "reserve":
                        walker.Move(word.Length);
                        walker.MoveToProcedure();
                        var key = TokenizeCode(walker)?.Value;
                        if (!string.IsNullOrEmpty(key))
                        {
                            walker.MoveToProcedure();
                            this.SwitchToken = TokenType.IdentifierKeyword;
                            var m = TokenizeCode(walker)?.Value;
                            if (!string.IsNullOrEmpty(m))
                            {
                                switch (m.ToLower())
                                {
                                    case ";":
                                        try { (this.Compiler as dynamic)?.Reserves?.Add(key, ""); } catch { }
                                        return new Token(TokenType.Comment, $"Reserved \"{key}\" as a noise", location);
                                    case "=":
                                    case "be":
                                    case "as":
                                        walker.MoveToProcedure();
                                        var val = TokenizeCode(walker)?.Value ?? "";
                                        try { (this.Compiler as dynamic)?.Reserves?.Add(key, val); } catch { }
                                        walker.MoveToProcedure();
                                        if (walker.Walk() != ';') walker.Move(-1);
                                        return new Token(TokenType.Comment, $"Reserved \"{key}\" as the {System.Text.Json.JsonSerializer.Serialize(val)}", location);
                                    default:
                                        walker.Reset(position);
                                        break;
                                }
                            }
                            else walker.Reset(position);
                        }
                        else walker.Reset(position);
                        break;
                    case "all":
                    case "one":
                    case "on":
                        ReservesScrubber(word);
                        walker.Move(word.Length);
                        word = (this.Compiler as dynamic)?.GetCommand(word) ?? word;
                        walker.MoveToProcedure();
                        if (Regex.IsMatch(walker.PeekProcedure(), "^[\\w\\#\\.]")) this.SwitchToken = TokenType.StringData;
                        return new Token(TokenType.FunctionKeyword, word, location);
                }

                var current = walker.Current;
                if (current != default(char))
                {
                    location = walker.Location;
                    word = string.Concat(walker.PeekUntil(ch => Regex.IsMatch(ch.ToString(), "[^A-Z0-9_$]", RegexOptions.IgnoreCase)).ToArray());
                    var lword = word.ToLower();

                    string c = null;
                    try { c = (this.Compiler as dynamic)?.GetCommand(word); } catch { }
                    if (c != null)
                    {
                        walker.Move(word.Length);
                        return new Token(TokenType.FunctionKeyword, c, location);
                    }

                    switch (lword)
                    {
                        case "each":
                            ReservesScrubber(word);
                            walker.Move(word.Length);
                            return new Token(TokenType.Statement, word, location);
                        case "select":
                        case "collect":
                            ReservesScrubber(word);
                            walker.Move(word.Length);
                            word = (this.Compiler as dynamic)?.GetCommand(word) ?? word;
                            walker.MoveToProcedure();
                            if (Regex.IsMatch(walker.PeekProcedure(), "\\*")) this.SwitchToken = TokenType.StringData;
                            return new Token(TokenType.Structure, word, location);
                        case "command":
                        case "as":
                        case "where":
                        case "order":
                        case "distinct":
                        case "limit":
                        case "sort":
                        case "asc":
                        case "reverse":
                        case "desc":
                        case "join":
                        case "concat":
                        case "flat":
                        case "fill":
                        case "at":
                        case "map":
                        case "find":
                        case "keys":
                        case "values":
                        case "length":
                        case "do":
                        case "doing":
                        case "end":
                        case "promise":
                        case "then":
                        case "otherwise":
                        case "anyway":
                            ReservesScrubber(word);
                            walker.Move(word.Length);
                            return new Token(TokenType.Structure, word, location);
                        case "is":
                        case "be":
                        case "not":
                        case "equal":
                        case "equals":
                            ReservesScrubber(word);
                            walker.Move(word.Length);
                            return new Token(TokenType.OperatorSymbol, word, location);
                        case "its":
                            ReservesScrubber(word);
                            walker.Move(word.Length);
                            return new Token(TokenType.IdentifierKeyword, "(data??this)", location);
                        case "data":
                            ReservesScrubber(word);
                            walker.Move(word.Length);
                            return new Token(TokenType.IdentifierKeyword, "data", location);
                        case "empty":
                            ReservesScrubber(word);
                            walker.Move(word.Length);
                            return new Token(TokenType.StringData, "", location);
                        default:
                            // fallthrough to further logic
                            break;
                    }

                    if (ReservesApplier(walker, word, location))
                        return TokenizeCode(walker);

                    if (current == '\\')
                        return TokenizeXPath(walker, location);

                    if (current == '#')
                    {
                        foreach (var ch in walker.WalkWhile(c => c == '#')) { }
                        return new Token(TokenType.Statement, "#", location);
                    }

                    var p = string.Concat(walker.PeekUntil(ch => char.IsWhiteSpace(ch)).ToArray());
                    if (Regex.IsMatch(p, "\\w+\\:((\\/\\/?\\w+.+)|([^\\:\\/].+))"))
                        return TokenizePath(walker, location);
                }
            }

            return base.TokenizeCode(walker);
        }

        protected int UsePath(string path)
        {
            if (Regex.IsMatch(path, "^\\w+:\\/\\/.+$")) return UseUrl(path);
            var val = (this.Compiler as DaRQ.Compiler.Compiler)?.ResourceProvider?.Resolve(path, (this.Compiler as DaRQ.Compiler.Compiler)?.Input?.Source != null ? (this.Compiler as DaRQ.Compiler.Compiler)?.ResourceProvider?.DirectoryName((this.Compiler as DaRQ.Compiler.Compiler)?.Input?.Source) : null);
            if (val != null && val.EndsWith("\\")) return UseFolder(val);
            else return UseFile(val) != 0 ? 1 : UseFolder(val);
        }

        protected int UseUrl(string url)
        {
            return 0;
        }

        protected int UseFolder(string folder)
        {
            folder = Regex.Replace(folder, "[\\/\\\\]$", "");
            int num = 0;
            try
            {
                var rp = (this.Compiler as DaRQ.Compiler.Compiler)?.ResourceProvider;
                if (rp != null && rp.Exists(folder))
                {
                    if (UseFile(folder + "\\index") != 0) return 1;
                    else
                    {
                        foreach (var adrs in rp.GetFolderContents(folder))
                        {
                            if (adrs.IsFile) num += UseFile(adrs.Path);
                            else if (adrs.IsFolder) num += UseFolder(adrs.Path);
                        }
                    }
                }
            }
            catch { }
            return num;
        }

        protected int UseFile(string file)
        {
            if (!Regex.IsMatch(file ?? "", "\\.(darq|js)$", RegexOptions.IgnoreCase))
                return UseFile((file ?? "") + ".darq") != 0 ? 1 : UseFile((file ?? "") + ".js");
            file = System.IO.Path.GetFullPath(file ?? "");
            try
            {
                var rp = (this.Compiler as DaRQ.Compiler.Compiler)?.ResourceProvider;
                var modules = (this.Compiler as dynamic)?.Modules;
                if (modules != null && !modules.ContainsKey(file) && rp != null && rp.Exists(file))
                {
                    var content = rp.GetFileContents(file, System.Text.Encoding.UTF8);
                    (this.Compiler as dynamic)?.Modules?.Add(file, (this.Compiler as DaRQ.Compiler.Compiler)?.Compile(new Input(content, file)));
                    return 1;
                }
            }
            catch { }
            return 0;
        }

        protected Token TokenizeXPath(CodeWalker walker, Location location)
        {
            walker.Walk();
            var value = string.Concat(walker.WalkUntil(ch => ch == '\\' && walker.Peek(-1) != (this.Compiler as dynamic)?.Options?.Escape).ToArray());
            walker.Walk();
            return new Token(TokenType.XPathData, value, location);
        }

        protected Token TokenizePath(CodeWalker walker, Location location)
        {
            var value = string.Concat(walker.WalkUntil(ch => char.IsWhiteSpace(ch)).ToArray());
            return new Token(TokenType.PathData, value, location);
        }

        protected bool ReservesApplier(CodeWalker walker, string word, Location location)
        {
            try
            {
                var reserves = (this.Compiler as dynamic)?.Reserves;
                if (reserves != null)
                {
                    foreach (var reserve in reserves)
                    {
                        if (word == reserve.Key)
                        {
                            walker.Replace(location.Index, word.Length, reserve.Value.ToCharArray());
                            return true;
                        }
                    }
                    var lw = word.ToLower();
                    foreach (var reserve in reserves)
                    {
                        if (lw == reserve.Key.ToLower())
                        {
                            walker.Replace(location.Index, word.Length, reserve.Value.ToCharArray());
                            return true;
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        protected int ReservesScrubber(string word)
        {
            word = word.ToLower();
            int d = 0;
            try
            {
                var reserves = (this.Compiler as dynamic)?.Reserves;
                if (reserves != null)
                {
                    var keys = reserves.Keys.ToArray();
                    for (int index = 0; index < keys.Length; index++)
                        if (keys[index].ToLower() == word)
                            if (reserves.Remove(keys[index])) d++;
                }
            }
            catch { }
            return d;
        }
    }
}
