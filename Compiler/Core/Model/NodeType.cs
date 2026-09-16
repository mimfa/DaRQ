// Converted from src/engine/DaRQ/Compiler/Core/NodeType.ts
using System;

namespace MiMFa.Compiler.Model
{
    [Flags]
    public enum NodeType
    {
        Unknown = 0,
        None = 1 << 0,
        Program = 1 << 1,
        Section = 1 << 2,
        Compute = 1 << 3,
        Procedure = 1 << 4,
        Plain = 1 << 5,
        Rule = 1 << 6,
        Selector = (1 << 11) | Rule,
        NormalSelector = (1 << 12) | Selector,
        ShortSelector = (1 << 13) | Selector,
        LongSelector = (1 << 14) | Selector,
        Iterator = (1 << 15) | Rule,
        ComputationIterator = (1 << 16) | Iterator,
        CollectionIterator = (1 << 17) | Iterator,
        ConditionIterator = (1 << 18) | Iterator,
        PostConditionIterator = (1 << 19) | ConditionIterator,
        Define = 1 << 7,
        Call = 1 << 8,
        Block = 1 << 9,
        Helper = 1 << 10
    }
}
