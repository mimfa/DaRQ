// Converted from src/engine/DaRQ/Compiler/Core/NodeType.ts
using System;

namespace MiMFa.DaRQ.Compiler.Core
{
    [Flags]
    public enum NodeType
    {
        Unknown = 0,
        None = 1 << 0,
        Program = 1 << 1,
        Compute = 1 << 2,
        Procedure = 1 << 3,
        Plain = 1 << 4,
        Rule = 1 << 5,
        Selector = (1 << 10) | Rule,
        NormalSelector = (1 << 11) | Selector,
        ShortSelector = (1 << 12) | Selector,
        LongSelector = (1 << 13) | Selector,
        Iterator = (1 << 14) | Rule,
        ComputationIterator = (1 << 15) | Iterator,
        CollectionIterator = (1 << 16) | Iterator,
        ConditionIterator = (1 << 17) | Iterator,
        PostConditionIterator = (1 << 18) | ConditionIterator,
        Define = 1 << 6,
        Call = 1 << 7,
        Block = 1 << 8,
        Helper = 1 << 9
    }
}
