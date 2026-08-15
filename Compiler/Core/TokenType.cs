// Converted from src/engine/DaRQ/Compiler/Core/TokenType.ts
using System;

namespace DaRQ.Compiler.Core
{
    [Flags]
    public enum TokenType
    {
        Unknown = 0,
        None = 1 << 0,

        Statement = 1 << 1,

        Symbol = 1 << 2,
        OperatorSymbol = (1 << 10) | Symbol,
        ConcatenatorSymbol = (1 << 11) | Symbol,
        SeparatorSymbol = (1 << 12) | Symbol,

        Scope = 1 << 3,
        StartScope = (1 << 13) | Scope,
        EndScope = (1 << 14) | Scope,

        Access = 1 << 4,

        Structure = 1 << 5,

        Data = 1 << 6,
        UndefinedData = (1 << 15) | Data,
        NullData = (1 << 16) | Data,
        BinaryData = (1 << 17) | Data,
        NumberData = (1 << 18) | Data,
        BooleanData = (1 << 19) | Data,
        StringData = (1 << 20) | Data,
        TemplateStringData = (1 << 21) | StringData,
        ObjectData = (1 << 22) | Data,
        ArrayData = (1 << 23) | Data,
        PathData = (1 << 24) | Data,
        XPathData = (1 << 25) | PathData,
        RegExPathData = (1 << 26) | PathData,

        Keyword = 1 << 7,
        NamespaceKeyword = (1 << 27) | Keyword,
        FunctionKeyword = (1 << 28) | Keyword,
        IdentifierKeyword = (1 << 29) | Keyword,

        Facilitator = 1 << 8,

        Comment = 1 << 9
    }
}
