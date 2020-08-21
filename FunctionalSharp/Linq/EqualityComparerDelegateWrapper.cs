using System;
using System.Collections.Generic;

namespace FunctionalSharp.Linq
{
    internal class EqualityComparerDelegateWrapper<T> : IEqualityComparer<T>
    {
        private readonly Func<T, T, bool> _comparerDelegate;
        private readonly Func<T, int> _hashCodeComparer;

        private readonly Func<T, int> hashCoder = obj => obj.GetHashCode();

        public EqualityComparerDelegateWrapper(Func<T, T, bool> comparer, Func<T, int> hashCodeComparer = null)
        {
            _comparerDelegate = comparer;
            _hashCodeComparer = hashCodeComparer ?? hashCoder;
        }

        public bool Equals(T x, T y) => _comparerDelegate(x, y);

        public int GetHashCode(T obj) => _hashCodeComparer(obj);
    }
}