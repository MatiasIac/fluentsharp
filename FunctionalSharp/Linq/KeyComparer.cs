using System;
using System.Collections.Generic;

namespace FunctionalSharp.Linq;

/// <summary>Creates reusable equality comparers from keys for LINQ and hash-based collections.</summary>
public static class KeyComparer
{
    /// <summary>Compares and hashes non-null values by their selected keys.</summary>
    /// <remarks>Null source values equal only other null source values; their keys are never selected.
    /// Null keys hash to zero. Keys and their equality/hash behavior must remain stable while in a hash collection.</remarks>
    public static IEqualityComparer<T> By<T, TKey>(Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        return new SelectedKeyComparer<T, TKey>(keySelector, comparer ?? EqualityComparer<TKey>.Default);
    }

    private sealed class SelectedKeyComparer<T, TKey>(Func<T, TKey> select, IEqualityComparer<TKey> comparer) : IEqualityComparer<T>
    {
        public bool Equals(T? x, T? y)
        {
            if (x is null) return y is null;
            if (y is null) return false;
            return comparer.Equals(select(x), select(y));
        }

        public int GetHashCode(T value)
        {
            if (value is null) return 0;
            var key = select(value);
            return key is null ? 0 : comparer.GetHashCode(key);
        }
    }
}
