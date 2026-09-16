using MiMFa.Compiler.Model;
using MiMFa.Compiler.Resource;
using System;
using System.Collections.Generic;

namespace MiMFa.Compiler.JavaScript
{
    public class Compiler : MiMFa.Compiler.Compiler
    {
        public Dictionary<string, string> Libraries { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, Token> Keywords { get; } = new Dictionary<string, Token>();


        public Compiler(IStage[] stages = null, Options options = null, ResourceProvider resourceProvider = null)
            : base(stages??new IStage[] {
                    new Tokenizer(),
                    new Preprocessor(),
                    new Parser(),
                    new Assembler(),
                    new Generator()
                }, options ?? new Options(), resourceProvider??new ResourceProvider())
        {
        }


        public virtual Node SetKeyword(string name, Node node)
        {
            Keywords[name] = node.Token;
            return node;
        }
        public virtual Token SetKeyword(Token token)
        {
            Keywords[token.Value] = token;
            return token;
        }
        public virtual string GetKeywordName(string name)
        {
            if (Keywords.ContainsKey(name)) return name;
            return null;
        }
        public virtual Token GetKeyword(string name)
        {
            if(Keywords.ContainsKey(name)) return Keywords[name];
            return null;
        }
    }
}
