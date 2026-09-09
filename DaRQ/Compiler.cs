using MiMFa.Compiler.Resource;
using System;
using System.Collections.Generic;

namespace MiMFa.Compiler.DaRQ
{
    public class Compiler : MiMFa.Compiler.Compiler
    {
        public Dictionary<string, string> Libraries { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> ActionCommands { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> FunctionCommands { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> DefinitionCommands { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> Reserves { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> Functions { get; } = new Dictionary<string, string>();


        public Compiler(Options options = null) : base(new IStage[] {
            new Tokenizer(),
            new Preprocessor(),
            new Parser(),
            new Assembler(),
            new Generator()
        }, options ?? new Options(), new ResourceProvider())
        {
        }


        public string SetFunction(string name)
        {
            Functions[name] = name;
            return name;
        }
        public string GetFunction(string name)
        {
            Functions.TryGetValue(name, out var v);
            return v;
        }

        public string SetActionCommand(string name)
        {
            ActionCommands[name.ToLower()] = name + "()";
            return name;
        }
        public string GetActionCommand(string name)
        {
            ActionCommands.TryGetValue(name.ToLower(), out string label);
            return label;
        }

        public string SetFunctionCommand(string name)
        {
            FunctionCommands[name.ToLower()] = name;
            return name;
        }
        public string GetFunctionCommand(string name)
        {
            FunctionCommands.TryGetValue(name.ToLower(), out string v);
            return v;
        }

        public string SetDirectionCommand(string name)
        {
            string key = name.ToLower();
            DefinitionCommands[key] = name;
            return name;
        }
        public string GetDirectionCommand(string name)
        {
            DefinitionCommands.TryGetValue(name.ToLower(), out string v);
            return v;
        }
    }
}
