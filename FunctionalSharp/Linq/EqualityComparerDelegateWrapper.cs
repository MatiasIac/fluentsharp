using System;
using System.Collections.Generic;

namespace FunctionalSharp.Linq
{
    internal class EqualityComparerDelegateWrapper<T> : IEqualityComparer<T>
    {
        private readonly Func<T, T, bool> _comparerDelegate;

        public EqualityComparerDelegateWrapper(Func<T, T, bool> comparer)
        {
            _comparerDelegate = comparer;
        }

        public bool Equals(T x, T y) => _comparerDelegate(x, y);

        public int GetHashCode(T obj) => obj.GetHashCode();
    }
}