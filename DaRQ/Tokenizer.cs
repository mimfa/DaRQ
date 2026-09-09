using System;
using System.Linq;
using System.Text.RegularExpressions;
using MiMFa.Compiler;
using MiMFa.Compiler.Tokenizer;
using MiMFa.Compiler.Core;
using MiMFa.Compiler.Model;
using MiMFa.Compiler.Walker;
using System.Collections.Generic;

namespace MiMFa.Compiler.DaRQ
{
    public class Tokenizer : MiMFa.Compiler.JavaScript.Tokenizer
    {
        public new Compiler Compiler { get; set; }

        public override bool Initialize(MiMFa.Compiler.Compiler compiler)
        {
            if (compiler != null) base.Initialize(Compiler = compiler == null ? Compiler : compiler as Compiler);
            return true;
        }

        protected TokenType? SwitchToken { get; set; }
        protected bool SwitchNamespace { get; set; } = false;

        protected override Token TokenizeCode(CodeWalker walker)
        {
            if (SpaceSwitch != null)
            {
                var sw = SpaceSwitch;
                SpaceSwitch = null;
                return new Token(sw, walker.WalkToProcedure());
            }
            walker.MoveToProcedure();
            if (this.SwitchToken != null)
            {
                var type = this.SwitchToken.Value;
                this.SwitchToken = null;
                return new Token(type, walker.WalkProcedure(), walker.Location);
            }
            var switchNameSpace = this.SwitchNamespace;
            if (this.SwitchNamespace) this.SwitchNamespace = false;
            if (walker.Current == "." && !walker.PeekProcedure().StartsWith("...")) this.SwitchNamespace = true;
            else if (!switchNameSpace && walker.Current != null)
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
                            int libs = UsePath(src);
                            if (walker.PeekProcedure() == ";" ) walker.WalkProcedure();
                            //walker.Replace(location.Index, p == ";"? walker.MoveToProcedure().Location.Index - location.Index : walker.Location.Index - location.Index, res.ToCharArray().Select(c => c.ToString()).ToArray());
                            return new Token(TokenType.Comment, libs<=0? $"// Could not find the {Newtonsoft.Json.JsonConvert.ToString(src)} library" : (libs > 1 ? $"// Used {libs} libraries from {Newtonsoft.Json.JsonConvert.ToString(src)}" : $"// Used {Newtonsoft.Json.JsonConvert.ToString(src)} library"), location);
                        }
                        else walker.Reset(position);
                        break;
                    case "reserve":
                        walker.Move(word.Length);
                        walker.MoveToProcedure();
                        var key = string.Join("", walker.WalkUntil(c=> Regex.IsMatch(c,"[\\s=;,]")));
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
                                        if(Compiler != null) try { Compiler.Reserves[key] = ""; } catch { }
                                        return new Token(TokenType.Comment, $"// Reserved {Newtonsoft.Json.JsonConvert.ToString(key)} as a noise", location);
                                    case "=":
                                    case "be":
                                    case "as":
                                        walker.MoveToProcedure();
                                        var val = TokenizeCode(walker)?.Value ?? "";
                                        try { Compiler?.Reserves.Add(key, val); } catch { }
                                        walker.MoveToProcedure();
                                        if (walker.Walk() != ";") walker.Move(-1);
                                        return new Token(TokenType.Comment, $"/* Reserved {Newtonsoft.Json.JsonConvert.ToString(key)} as the {Newtonsoft.Json.JsonConvert.ToString(val.Replace("*/", "*\\/"))} */{Environment.NewLine}", location);
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
                        word = Compiler?.GetFunctionCommand(word) ?? word;
                        walker.MoveToProcedure();
                        if(walker.PeekProcedure() != "(" && Regex.IsMatch(walker.PeekProcedure(), "^[\\w\\#\\.]")) this.SwitchToken = TokenType.StringData;
                        return new Token(TokenType.FunctionKeyword, word, location);
                }

                var current = walker.Current;
                if (current != null)
                {
                    location = walker.Location;
                    word = string.Concat(walker.PeekUntil(ch => Regex.IsMatch(ch.ToString(), "[^A-Z0-9_$]", RegexOptions.IgnoreCase)).ToArray());
                    var lword = word.ToLower();

                    string c = null;
                    try { c = this.Compiler?.GetFunctionCommand(word); } catch { }
                    if (c != null)
                    {
                        walker.Move(word.Length);
                        return new Token(TokenType.FunctionKeyword, c, location);
                    }

                    switch (lword)
                    {
                        case "command":
                        case "each":
                            ReservesScrubber(word);
                            walker.Move(word.Length);
                            return new Token(TokenType.Statement, word, location);
                        case "as":
                            ReservesScrubber(word);
                            walker.Move(word.Length);
                            return new Token(TokenType.Structure, word, location);
                        case "select":
                        case "collect":
                            ReservesScrubber(word);
                            walker.Move(word.Length);
                            word = Compiler?.GetFunctionCommand(word) ?? word;
                            walker.MoveToProcedure();
                            if (Regex.IsMatch(walker.PeekProcedure(), "\\*")) this.SwitchToken = TokenType.StringData;
                            return new Token(TokenType.Structure | TokenType.FunctionKeyword, word, location);
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
                            ReservesScrubber(word);
                            walker.Move(word.Length);
                            return new Token(TokenType.Structure | TokenType.FunctionKeyword, word, location);
                        case "do":
                        case "doing":
                        case "end":
                            ReservesScrubber(word);
                            walker.Move(word.Length);
                            return new Token(TokenType.Structure, word, location);
                        case "promise":
                            ReservesScrubber(word);
                            walker.Move(word.Length);
                            return new Token(TokenType.Structure, word, location);
                        case "then":
                        case "otherwise":
                        case "anyway":
                            ReservesScrubber(word);
                            walker.Move(word.Length);
                            return new Token(TokenType.Structure | TokenType.FunctionKeyword, word, location);
                        case "is":
                        case "be":
                        case "not":
                        case "equal":
                        case "equals":
                            ReservesScrubber(word);
                            walker.Move(word.Length);
                            return new Token(TokenType.OperatorSymbol, word, location);
                        case "to":
                        case "from":
                            ReservesScrubber(word);
                            walker.Move(word.Length);
                            return new Token(TokenType.SeparatorSymbol, ",", location);
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

                        //#region JS Statements
                        case "if":
                        case "else":
                        case "switch":
                        case "case":
                        case "default":

                        case "for":
                        case "of":
                        case "in":
                        case "while":
                        case "break":
                        case "continue":

                        case "try":
                        case "catch":
                        case "finally":
                        case "throw":

                        case "return":
                        case "yield":
                        case "await":
                        case "async":

                        case "function":
                        case "class":
                        case "extends":
                        case "super":
                        case "new":
                        case "this":

                        case "import":
                        case "export":

                        case "const":
                        case "let":
                        case "var":

                        case "true":
                        case "false":
                        case "undefined":
                        case "null":

                        case "void":
                        case "delete":
                        case "typeof":
                        case "instanceof":

                        case "with":
                        case "debugger":

                        case "get":
                        case "set":

                        case "static":

                        case "implements":
                        case "interface":
                        case "package":
                        case "private":
                        case "protected":
                        case "public":

                        case "enum":
                            //#endregion
                            ReservesScrubber(word);
                            walker.Replace(position, word.Length, lword.ToCharArray().Select(v=>v.ToString()).ToArray());
                            break;
                    }

                    if (ReservesApplier(walker, word, location))
                        return TokenizeCode(walker);

                    if (current == "\\")
                        return TokenizeXPath(walker, location);

                    if (current == "#")
                    {
                        walker.WalkWhile(w => w == "#").ToArray();
                        return new Token(TokenType.Statement, "#", location);
                    }

                    if (current == ":")
                        return new Token(TokenType.SeparatorSymbol, walker.Walk(), location);

                    var p = string.Concat(walker.PeekUntil(ch => string.IsNullOrWhiteSpace(ch)).ToArray());
                    if (Regex.IsMatch(p, "\\w+\\:((\\/\\/?\\w+.+)|([^\\:\\/].+))"))
                        return TokenizePath(walker, location);
                }
            }

            return base.TokenizeCode(walker);
        }

        protected virtual int UsePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return 0;
            if (Regex.IsMatch(path, "^\\w+:\\/\\/.+$")) return UseUrl(path);
            var compiler = this.Compiler as MiMFa.Compiler.Compiler;
            if (compiler == null) return 0;
            var source = compiler.Input?.Source??"DaRQ\\Library\\index";
            var baseDirectory = source != null ? compiler?.ResourceProvider?.DirectoryName(source) : null;
            var val = compiler?.ResourceProvider?.Resolve(path, baseDirectory);
            if (!string.IsNullOrEmpty(val) && val.EndsWith("\\")) return UseFolder(val);
            int num = 0;
            return (num = UseFile(val))>0?num:UseFolder(val);
        }

        protected virtual int UseUrl(string url)
        {
            return 0;
        }

        protected virtual int UseFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder)) return 0;
            folder = Regex.Replace(folder, "[\\/\\\\]$", "");
            int results = 0;
            try
            {
                var rp = (this.Compiler as MiMFa.Compiler.Compiler)?.ResourceProvider;
                if (rp != null && rp.Exists(folder))
                {
                    var res = UseFile(folder + "\\index");
                    if (res>0) return res;
                    else
                    {
                        foreach (var adrs in rp.GetFolderContents(folder))
                        {
                            if (adrs.IsFile) results += UseFile(adrs.Path);
                            else if (adrs.IsFolder) results += UseFolder(adrs.Path);
                        }
                    }
                }
            }
            catch { }
            return results;
        }

        protected virtual int UseFile(string file)
        {
            if (string.IsNullOrWhiteSpace(file)) return 0;
            int num = 0;
            if (!Regex.IsMatch(file, "\\.(darq|js)$", RegexOptions.IgnoreCase))
                return (num = UseFile(file + ".darq"))>0? num : UseFile(file + ".js");
            file = System.IO.Path.GetFullPath(file);
            try
            {
                var compiler = this.Compiler as MiMFa.Compiler.Compiler;
                var rp = compiler?.ResourceProvider;
                var modules = (compiler as MiMFa.Compiler.DaRQ.Compiler)?.Libraries;
                if (modules != null && !modules.ContainsKey(file) && rp != null && rp.Exists(file))
                {
                    Input input = compiler.Input;
                    Output output = compiler.Output;
                    (compiler as MiMFa.Compiler.DaRQ.Compiler)?.Libraries.Add(
                        file,
                        (compiler as MiMFa.Compiler.DaRQ.Compiler).Compile(new Input(rp.GetFileContents(file, System.Text.Encoding.UTF8), file)).Content);
                    compiler.Input = input;
                    compiler.Output = output;
                    return 1;
                }
            }
            catch { }
            return num;
        }

        protected virtual Token TokenizeXPath(CodeWalker walker, Location location)
        {
            walker.Walk();
            var value = string.Concat(walker.WalkUntil(ch => ch == "\\" && walker.Peek(-1) != Compiler?.Options?.Escape).ToArray());
            walker.Walk();
            return new Token(TokenType.XPathData, value, location);
        }

        protected virtual Token TokenizePath(CodeWalker walker, Location location)
        {
            var value = string.Concat(walker.WalkUntil(ch => string.IsNullOrWhiteSpace(ch)).ToArray());
            return new Token(TokenType.PathData, value, location);
        }

        protected virtual bool ReservesApplier(CodeWalker walker, string word, Location location)
        {
            try
            {
                var reserves = Compiler?.Reserves;
                if (reserves != null)
                {
                    foreach (var reserve in reserves)
                    {
                        if (word == reserve.Key)
                        {
                            if (word == reserve.Value) SwitchNamespace = true;
                            else walker.Replace(location.Index, word.Length, reserve.Value.ToCharArray().Select(v=>v.ToString()).ToArray());
                            return true;
                        }
                    }
                    var lw = word.ToLower();
                    foreach (var reserve in reserves)
                    {
                        if (lw == reserve.Key.ToLower())
                        {
                            if (word == reserve.Value) SwitchNamespace = true;
                            else walker.Replace(location.Index, word.Length, reserve.Value.ToCharArray().Select(v => v.ToString()).ToArray());
                            return true;
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        protected virtual int ReservesScrubber(string word)
        {
            word = word.ToLower();
            int d = 0;
            try
            {
                var reserves = Compiler?.Reserves;
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
