using MiMFa.Compiler.Model;
using MiMFa.Compiler.Resource;
using System;
using System.Collections.Generic;

namespace MiMFa.Compiler.DaRQ
{
    public class Compiler : JavaScript.Compiler
    {
        public Dictionary<string, string> ActionCommands { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> FunctionCommands { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> DefinitionCommands { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> Reserves { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public Compiler(IStage[] stages = null, Options options = null, ResourceProvider resourceProvider = null)
            : base(stages ?? new IStage[] {
                    new Tokenizer(),
                    new Preprocessor(),
                    new Parser(),
                    new Assembler(),
                    new Generator()
                }, options ?? new Options(), resourceProvider ?? new ResourceProvider())
        {
        }

        public virtual Node SetActionCommand(string name, Node node)
        {
            ActionCommands[name.ToLower()] = name + "()";
            return SetKeyword(name, node);
        }
        public virtual Token GetActionCommand(string name)
        {
            ActionCommands.TryGetValue(name.ToLower(), out string v);
            return GetKeyword(v);
        }
        public virtual string GetActionCommandName(string name)
        {
            ActionCommands.TryGetValue(name.ToLower(), out string label);
            return label;
        }

        public virtual Node SetFunctionCommand(string name, Node node)
        {
            FunctionCommands[name.ToLower()] = name;
            return SetKeyword(name, node);
        }
        public virtual Token GetFunctionCommand(string name)
        {
            FunctionCommands.TryGetValue(name.ToLower(), out string v);
            return GetKeyword(v);
        }
        public virtual string GetFunctionCommandName(string name)
        {
            FunctionCommands.TryGetValue(name.ToLower(), out string v);
            return v;
        }

        public virtual Node SetDefinitionCommand(string name, Node node)
        {
            string key = name.ToLower();
            DefinitionCommands[key] = name;
            return SetKeyword(name, node);
        }
        public virtual Token GetDefinitionCommand(string name)
        {
            DefinitionCommands.TryGetValue(name.ToLower(), out string v);
            return GetKeyword(v);
        }
        public virtual string GetDefinitionCommandName(string name)
        {
            DefinitionCommands.TryGetValue(name.ToLower(), out string v);
            return v;
        }

        public virtual Token GetCommand(string name)
        {
            return GetFunctionCommand(name) ?? GetDefinitionCommand(name) ?? GetActionCommand(name);
        }
        public virtual string GetCommandName(string name)
        {
            return GetFunctionCommandName(name) ?? GetDefinitionCommandName(name) ?? GetActionCommandName(name);
        }

        public virtual string GetFunctionName(string name)
        {
            if (Keywords.ContainsKey(name) && Keywords[name]?.Is(TokenType.FunctionKeyword) == true)
                return name;
            return null;
        }

    }
}
