using System;
using System.Collections.Generic;

namespace MiMFa.DaRQ.Compiler.Core
{
    public class Walker<T> : WalkerBase<T>
    {
        public Walker(IEnumerable<T> items, string? source = null) : base(items == null ? new T[0] : System.Linq.Enumerable.ToArray(items), source)
        {
        }

        public Walker(T[] items, string? source = null) : base(items ?? new T[0], source)
        {
        }
    }
}
