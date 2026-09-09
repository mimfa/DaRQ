// Converted from src/engine/DaRQ/Compiler/IStage.ts
using System;

namespace MiMFa.Compiler
{
    public interface IStage
    {
        Compiler Compiler { get; set; }

        bool Initialize(Compiler compiler);

        object Transform(object input, Compiler compiler);
    }
}
