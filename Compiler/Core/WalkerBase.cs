// Converted from src/engine/DaRQ/Compiler/Core/WalkerBase.ts
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiMFa.DaRQ.Compiler.Core
{
    public interface IWalker<T>
    {
        string? Source { get; }
        int Position { get; }
        int Length { get; }
        bool IsRunning { get; }
        bool IsEnded { get; }
        T Current { get; }

        IWalker<T> Move(int count);
        IWalker<T> Reset(int position);
        IWalker<T> Remove(int start, int count);
    }

    public abstract class WalkerBase<T> : IWalker<T>
    {
        public string? Source { get; }

        protected T[] content;
        public T[] Content => content;

        protected int position = 0;
        public int Position => position;
        public int Length => content.Length;

        public T Current => Peek(0)!;

        public bool IsRunning => position >= 0 && position < content.Length;
        public bool IsEnded => position >= content.Length;

        protected WalkerBase(T[] content, string? source = null)
        {
            this.content = content ?? new T[0];
            this.Source = source;
        }

        public T Peek(int offset = 0)
        {
            var index = position + offset;
            if (index >= 0 && index < content.Length) return content[index];
            return default!;
        }

        public T PeekThe(Func<T, bool> aggregator, int offset = 0)
        {
            T p = default!;
            int c = 0;
            if (offset < 0)
            {
                while ((p = Peek(c)) != null)
                {
                    if (!aggregator(p)) offset--;
                    if (c-- <= offset) break;
                }
            }
            else
            {
                while ((p = Peek(c)) != null)
                {
                    if (!aggregator(p)) offset++;
                    if (c++ >= offset) break;
                }
            }
            return p;
        }

        public virtual T Walk()
        {
            if (IsEnded) return default!;
            return content[position++];
        }

        public T WalkTo(Func<T, bool> aggregator)
        {
            T p = default!;
            while (!aggregator(p = Walk()) && IsRunning) ;
            return p;
        }

        public IWalker<T> Move(int count = 1)
        {
            position = Math.Max(Math.Min(position + count, content.Length), 0);
            return this;
        }

        public IWalker<T> MoveTo(Func<T, bool> aggregator, int count = 1)
        {
            while (count > 0)
                if (!IsRunning || aggregator(Walk())) count--;
            return this;
        }

        public IWalker<T> Reset(int position = 0)
        {
            this.position = Math.Max(Math.Min(content.Length, position), 0);
            return this;
        }

        public IWalker<T> Remove(int start = -1, int count = 1)
        {
            return Replace(start < 0 ? position : start, count, new T[0]);
        }

        public IWalker<T> Replace(int start, int count, T[] replacement)
        {
            var list = new List<T>();
            list.AddRange(content.Take(start));
            list.AddRange(replacement);
            list.AddRange(content.Skip(start + count));
            content = list.ToArray();
            return this;
        }

        public IEnumerable<TOut> MapWhile<TOut>(Func<bool> predicate, Func<TOut> action)
        {
            while (predicate())
            {
                var m = action();
                if (m != null) yield return m;
            }
        }

        public IEnumerable<TOut> MapUntil<TOut>(Func<bool> predicate, Func<TOut> action)
        {
            while (!predicate())
            {
                var m = action();
                if (m != null) yield return m;
            }
        }

        public IEnumerable<T> PeekCount(int count = 1)
        {
            var length = Math.Max(0, Math.Min(content.Length, position + count));
            if (length > position)
                for (int index = position; index < length; index++) yield return content[index];
            else if (length < position)
                for (int index = length; index < position; index++) yield return content[index];
        }

        public IEnumerable<T> PeekWhile(Func<T, bool> predicate)
        {
            int offset = 0;
            T c;
            while ((c = Peek(offset++)) != null && predicate(c)) yield return c;
        }

        public IEnumerable<T> PeekUntil(Func<T, bool> predicate)
        {
            int offset = 0;
            T c;
            while ((c = Peek(offset++)) != null && !predicate(c)) yield return c;
        }

        public IEnumerable<T> WalkWhile(Func<T, bool> predicate)
        {
            while (!IsEnded)
            {
                var current = Current;
                if (current == null || !predicate(current)) break;
                yield return Walk();
            }
        }

        public IEnumerable<T> WalkUntil(Func<T, bool> predicate)
        {
            while (!IsEnded)
            {
                var current = Current;
                if (current == null || predicate(current)) break;
                yield return Walk();
            }
        }

        public IEnumerable<TOut> Cast<TOut>(Func<T, TOut> convertor)
        {
            T p = default;
            TOut o;
            while ((o = convertor(p = Walk())) != null) yield return o;
        }
    }
}
