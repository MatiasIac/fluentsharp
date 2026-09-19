using System;
using System.Collections.Generic;
using System.Linq;

namespace FunctionalSharp.Linq
{
    /// <summary>Provides LINQ overloads that accept delegates for equality comparison.</summary>
    /// <remarks>
    /// Comparers must define an equality relation. Hash-based operations use each value's existing
    /// GetHashCode unless a hash delegate is supplied; equal values must have equal hash codes.
    /// </remarks>
    public static class LinqExtenders
    {

        /// <summary>Returns distinct values present in both sequences using the supplied equality delegate.</summary>
        public static IEnumerable<T> Intersect<T>(this IEnumerable<T> source, IEnumerable<T> second, Func<T, T, bool> comparer)
            => source.Intersect(second, new EqualityComparerDelegateWrapper<T>(comparer));
        
        /// <summary>Returns distinct values in the first sequence that are absent from the second.</summary>
        public static IEnumerable<T> Except<T>(this IEnumerable<T> source, IEnumerable<T> second, Func<T, T, bool> comparer)
            => source.Except(second, new EqualityComparerDelegateWrapper<T>(comparer));

        /// <summary>Determines whether a sequence contains a value using the supplied equality delegate.</summary>
        public static bool Contains<T>(this IEnumerable<T> source, T value, Func<T, T, bool> comparer)
            => source.Contains(value, new EqualityComparerDelegateWrapper<T>(comparer));

        /// <summary>Returns distinct values using an equality delegate and each value's existing hash code.</summary>
        public static IEnumerable<T> Distinct<T>(this IEnumerable<T> source, Func<T, T, bool> comparer)
            => source.Distinct(new EqualityComparerDelegateWrapper<T>(comparer));

        /// <summary>Returns distinct values using matching equality and hash-code delegates.</summary>
        public static IEnumerable<T> Distinct<T>(this IEnumerable<T> source, 
            Func<T, T, bool> comparer, 
            Func<T, int> hashCodeComparer) where T : new()
            => source.Distinct(new EqualityComparerDelegateWrapper<T>(comparer, hashCodeComparer));

        /// <summary>Groups values by key and projects each group using the supplied key equality delegate.</summary>
        public static IEnumerable<TResult> GroupBy<TSource, TKey, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TKey, IEnumerable<TSource>, TResult> resultSelector,
            Func<TKey, TKey, bool> comparer)
            => source.GroupBy(keySelector, resultSelector, new EqualityComparerDelegateWrapper<TKey>(comparer));

        /// <summary>Groups projected elements by key and projects each group using a key equality delegate.</summary>
        public static IEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            Func<TKey, IEnumerable<TElement>, TResult> resultSelector,
            Func<TKey, TKey, bool> comparer)
            => source.GroupBy(keySelector, elementSelector, resultSelector, new EqualityComparerDelegateWrapper<TKey>(comparer));

        /// <summary>Groups values by keys compared with the supplied equality delegate.</summary>
        public static IEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector, 
            Func<TKey, TKey, bool> comparer)
            => source.GroupBy(keySelector, new EqualityComparerDelegateWrapper<TKey>(comparer));

        /// <summary>Groups projected elements by keys compared with the supplied equality delegate.</summary>
        public static IEnumerable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(
            this IEnumerable<TSource> source, 
            Func<TSource, TKey> keySelector, 
            Func<TSource, TElement> elementSelector,
            Func<TKey, TKey, bool> comparer)
            => source.GroupBy(keySelector, elementSelector, new EqualityComparerDelegateWrapper<TKey>(comparer));

        /// <summary>Joins each outer value to a group of inner values using a key equality delegate.</summary>
        public static IEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(
            this IEnumerable<TOuter> outer,
            IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector,
            Func<TOuter, IEnumerable<TInner>, TResult> resultSelector,
            Func<TKey, TKey, bool> comparer)
            => outer.GroupJoin(inner, outerKeySelector, innerKeySelector, resultSelector, new EqualityComparerDelegateWrapper<TKey>(comparer));

        /// <summary>Joins matching values from two sequences using a key equality delegate.</summary>
        public static IEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(
            this IEnumerable<TOuter> outer,
            IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector,
            Func<TKey, TKey, bool> comparer)
            => outer.Join(inner, outerKeySelector, innerKeySelector, resultSelector, new EqualityComparerDelegateWrapper<TKey>(comparer));

        /// <summary>Creates a dictionary of projected keys and values using a key equality delegate.</summary>
        public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            Func<TKey, TKey, bool> comparer)
            => source.ToDictionary(keySelector, elementSelector, new EqualityComparerDelegateWrapper<TKey>(comparer));

        /// <summary>Creates a lookup of projected elements using a key equality delegate.</summary>
        public static ILookup<TKey, TElement> ToLookup<TSource, TKey, TElement>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            Func<TKey, TKey, bool> comparer)
            => source.ToLookup(keySelector, elementSelector, new EqualityComparerDelegateWrapper<TKey>(comparer));

        /// <summary>Creates a lookup of source values using a key equality delegate.</summary>
        public static ILookup<TKey, TSource> ToLookup<TSource, TKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TKey, TKey, bool> comparer)
            => source.ToLookup(keySelector, new EqualityComparerDelegateWrapper<TKey>(comparer));

        /// <summary>Returns distinct values from both sequences using the supplied equality delegate.</summary>
        public static IEnumerable<TSource> Union<TSource>(
            this IEnumerable<TSource> first,
            IEnumerable<TSource> second,
            Func<TSource, TSource, bool> comparer)
            => first.Union(second, new EqualityComparerDelegateWrapper<TSource>(comparer));
    }
}
