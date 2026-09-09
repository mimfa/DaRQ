using System;

namespace MiMFa.Compiler.Model
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
            TerminatorSymbol = (1 << 13) | Symbol,

        Scope = 1 << 3,
            StartScope = (1 << 14) | Scope,
            EndScope = (1 << 15) | Scope,

        Access = 1 << 4,

        Structure = 1 << 5,

        Data = 1 << 6,
            UndefinedData = (1 << 16) | Data,
            NullData = (1 << 17) | Data,
            BinaryData = (1 << 18) | Data,
            NumberData = (1 << 19) | Data,
            BooleanData = (1 << 20) | Data,
            StringData = (1 << 21) | Data,
            TemplateStringData = (1 << 22) | StringData,
            ObjectData = (1 << 23) | Data,
            ArrayData = (1 << 24) | Data,
            PathData = (1 << 25) | Data,
            XPathData = (1 << 26) | PathData,
            RegExPathData = (1 << 27) | PathData,

        Keyword = 1 << 7,
            NamespaceKeyword = (1 << 28) | Keyword,
            FunctionKeyword = (1 << 29) | Keyword,
            IdentifierKeyword = (1 << 30) | Keyword,

        Facilitator = 1 << 8,

        Comment = 1 << 9
    }
}