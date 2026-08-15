// Converted from src/engine/DaRQ/DaRQ-Compiler/Generator.ts
using System;
using System.Collections.Generic;
using System.Linq;
using DaRQ.Compiler.Core;

namespace DaRQ.DaRQ_Compiler
{
    public class Generator : DaRQ.JavaScriptCompiler.Generator
    {
        public override string Transform(object input, DaRQ.Compiler.Compiler compiler)
        {
            var program = input as Program;
            var parts = new List<string>();
            try
            {
                var modules = (compiler as dynamic)?.Modules;
                if (modules != null && modules.Count > 0)
                {
                    parts.Add("//#region MODULES");
                    foreach (var e in modules)
                    {
                        parts.Add(e.Value.Content);
                    }
                    parts.Add("//#endregion");
                    parts.Add("\n");
                }
            }
            catch { }
            parts.Add(base.Transform(input, compiler));
            return string.Join("\n", parts.Where(p => p != null));
        }
    }
}
