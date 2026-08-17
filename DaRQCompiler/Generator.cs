// Converted from src/engine/DaRQ/DaRQ-Compiler/Generator.ts
using System;
using System.Collections.Generic;
using System.Linq;
using MiMFa.DaRQ.Compiler.Core;

namespace MiMFa.DaRQ.DaRQCompiler
{
    public class Generator : MiMFa.DaRQ.JavaScriptCompiler.Generator
    {
        public new DaRQCompiler? Compiler { get; set; }

        public object Transform(object input, DaRQCompiler? compiler)
        {
            var program = input as Program;
            var parts = new List<string>();
            try
            {
                var modules = compiler?.Modules;
                if (modules != null && modules?.Count > 0)
                {
                    parts.Add("//#region MODULES");
                    foreach (var e in modules)
                    {
                        parts.Add(e.Value.Content ?? string.Empty);
                    }
                    parts.Add("//#endregion");
                    parts.Add("\n");
                }
            }
            catch { }
            var baseRes = base.Transform(input, compiler);
            if (baseRes is System.Collections.IEnumerable be)
            {
                foreach (var item in be)
                    parts.Add(item?.ToString() ?? string.Empty);
            }
            else if (baseRes != null)
            {
                parts.Add(baseRes.ToString() ?? string.Empty);
            }

            return string.Join("\n", parts.Where(p => !string.IsNullOrEmpty(p)));
        }
    }
}
