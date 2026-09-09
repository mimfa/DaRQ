// Converted from src/engine/DaRQ/Compiler/ResourceProvider.ts
using System;
using System.Collections.Generic;

namespace MiMFa.Compiler.Resource
{
    public interface IResourceEntry
    {
        string Name { get; }
        string Path { get; }
        bool IsFile { get; }
        bool IsFolder { get; }
    }
}
