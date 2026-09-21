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
            if (name == null) return null;
            Keywords[name] = node.Token;
            return node;
        }
        public virtual Token SetKeyword(Token token)
        {
            if (token == null) return null;
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

        public virtual bool IsFlag(Node node) => node != null && node.Is(TokenType.End | TokenType.Scope);

        public virtual bool IsIndependent(Node node) => !node.Is(NodeType.BlockStructure, NodeType.CallStructure, NodeType.DefineStructure) && node.Is(NodeType.Program, NodeType.Structure, NodeType.Independ);
        public virtual bool IsDependent(Node node) => node.Is(NodeType.Append, NodeType.Prepend, NodeType.Depend, NodeType.Chunk, NodeType.BlockStructure, NodeType.CallStructure, NodeType.DefineStructure);
        public virtual bool IsAppendent(Node node) => node.Is(NodeType.Append, NodeType.Chunk, NodeType.BlockStructure, NodeType.CallStructure);
        public virtual bool IsPrependent(Node node) => !node.Is(NodeType.BlockStructure) && node.Is(NodeType.Prepend, NodeType.Chunk, NodeType.CallStructure, NodeType.DefineStructure);

        public virtual bool IsInitializers(Node node) => node != null && node.Is(TokenType.Start, TokenType.Prefix);
        public virtual bool IsConnectors(Node node) => node != null && (node.IsMatch("[", "(") || node.Is(TokenType.Middle | TokenType.Symbol) || node.Is(TokenType.Suffix | TokenType.Symbol));
        public virtual bool IsComplementors(Node node) => node != null && node.Is(TokenType.Suffix, TokenType.ConcatenatorSymbol);
        public virtual bool IsMediators(Node node) => node != null && node.Is(TokenType.Middle);
        public virtual bool IsSeparators(Node node) => node != null && node.Is(TokenType.DelimiterSymbol, TokenType.TerminatorSymbol);
        public virtual bool IsDelimiters(Node node) => node != null && node.Is(TokenType.DelimiterSymbol);
        public virtual bool IsFinalizers(Node node) => node != null && node.Is(TokenType.End, TokenType.TerminatorSymbol);
        public virtual bool IsOrganizers(Node node) => node != null && node.Is(TokenType.Statement, TokenType.TerminatorSymbol);

    }
}
