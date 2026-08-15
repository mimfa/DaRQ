// Converted from src/engine/DaRQ/Compiler/IStage.ts
using System;

namespace DaRQ.Compiler
{
    public interface IStage
    {
        Compiler Compiler { get; set; }

        object Transform(object input, Compiler compiler);
    }
}
