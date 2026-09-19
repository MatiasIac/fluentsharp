using System;
using System.Collections.Generic;

namespace FunctionalSharp.Linq
{
    internal class EqualityComparerDelegateWrapper<T> : IEqualityComparer<T>
    {
        private readonly Func<T?, T?, bool> _comparerDelegate;
        private readonly Func<T, int>? _hashCodeComparer;

        public EqualityComparerDelegateWrapper(Func<T?, T?, bool> comparer, Func<T, int>? hashCodeComparer = null)
        {
            _comparerDelegate = comparer ?? throw new ArgumentNullException(nameof(comparer));
            _hashCodeComparer = hashCodeComparer;
        }

        public bool Equals(T? x, T? y) => _comparerDelegate(x, y);

        // Arbitrary delegate equality need not agree with T.GetHashCode(). A constant hash
        // keeps equal values in the same bucket when no matching hash delegate is supplied.
        public int GetHashCode(T obj) => _hashCodeComparer?.Invoke(obj) ?? 0;
    }
}
