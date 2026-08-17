// Converted from src/engine/DaRQ/DaRQ-Compiler/index.ts
using System;
using System.Collections.Generic;
using MiMFa.DaRQ.Compiler;

namespace MiMFa.DaRQ.DaRQCompiler
{
    public class DaRQCompiler : MiMFa.DaRQ.Compiler.Compiler
    {
        public Dictionary<string, Output> Modules { get; } = new Dictionary<string, Output>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> CallLables { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> Commands { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // DaRQ-specific commands
            ["all"] = "ALL",
            ["one"] = "ONE",
            ["on"] = "ON",

            // All JS global functions
            ["parseint"] = "parseInt",
            ["parsefloat"] = "parseFloat",
            ["alert"] = "alert",
            ["confirm"] = "confirm",
            ["prompt"] = "prompt",
            ["fetch"] = "fetch",
            ["number"] = "Number",
            ["urldecode"] = "urlDecode",
            ["urlencode"] = "urlEncode",
            ["uriencode"] = "encodeURIComponent",
            ["uridecode"] = "decodeURIComponent",
            ["encodeuricomponent"] = "encodeURIComponent",
            ["decodeuricomponent"] = "decodeURIComponent",
            ["isnan"] = "isNaN",
            ["isfinite"] = "isFinite",
            ["istypeof"] = "typeof",
            ["maximum"] = "Math.max",
            ["minimum"] = "Math.min"
        };
        public Dictionary<string, string> Functions { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
        };
        public Dictionary<string, string> Reserves { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["and"] = "&&",
            ["or"] = "||",
            ["from"] = ",",
            ["to"] = ","
        };

        public DaRQCompiler(Options? options = null) : base(new IStage[] {
            new Tokenizer(),
            new Preprocessor(),
            new Parser(),
            new Assembler(),
            new Generator()
        }, options ?? new Options(), new ResourceProvider())
        {
        }

        public string SetCallLable(string name)
        {
            CallLables[name] = name;
            return name;
        }

        public string? GetCallLable(string name)
        {
            if (CallLables.TryGetValue(name, out var label)) return label + "()";
            return null;
        }

        public string SetCommand(string name)
        {
            Commands[name.ToLower()] = name;
            return name;
        }

        public string? GetCommand(string name)
        {
            Commands.TryGetValue(name.ToLower(), out var v);
            return v;
        }

        public string SetFunction(string name)
        {
            Functions[name] = name;
            return name;
        }

        public string? GetFunction(string name)
        {
            Functions.TryGetValue(name, out var v);
            return v;
        }
    }
}
