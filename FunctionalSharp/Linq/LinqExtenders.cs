using System;
using System.Collections.Generic;
using System.Linq;

namespace FunctionalSharp.Linq
{
    /// <summary>Provides LINQ overloads that accept delegates for equality comparison.</summary>
    /// <remarks>
    /// Comparers must define an equality relation and handle null values if present.
    /// Equality-only overloads use a constant hash so values considered equal are compared
    /// regardless of their existing hash codes. This can require quadratic work as sequences grow.
    /// For larger inputs, use matching equality and hash delegates, a key comparer, or a standard
    /// LINQ overload accepting an IEqualityComparer. Supplied hash delegates must return equal
    /// hash codes for values considered equal. A null hash delegate uses the constant-hash fallback.
    /// </remarks>
    public static class LinqExtenders
    {

        /// <summary>Returns distinct values present in both sequences using the supplied equality delegate.</summary>
        public static IEnumerable<T> Intersect<T>(this IEnumerable<T> source, IEnumerable<T> second, Func<T?, T?, bool> comparer, Func<T, int>? hashCodeComparer = null)
            => source.Intersect(second, new EqualityComparerDelegateWrapper<T>(comparer, hashCodeComparer));
        
        /// <summary>Returns distinct values in the first sequence that are absent from the second.</summary>
        public static IEnumerable<T> Except<T>(this IEnumerable<T> source, IEnumerable<T> second, Func<T?, T?, bool> comparer, Func<T, int>? hashCodeComparer = null)
            => source.Except(second, new EqualityComparerDelegateWrapper<T>(comparer, hashCodeComparer));

        /// <summary>Determines whether a sequence contains a value using the supplied equality delegate.</summary>
        public static bool Contains<T>(this IEnumerable<T> source, T value, Func<T?, T?, bool> comparer, Func<T, int>? hashCodeComparer = null)
            => source.Contains(value, new EqualityComparerDelegateWrapper<T>(comparer, hashCodeComparer));

        /// <summary>Returns distinct values using an equality delegate and a constant-hash fallback.</summary>
        public static IEnumerable<T> Distinct<T>(this IEnumerable<T> source, Func<T?, T?, bool> comparer, Func<T, int>? hashCodeComparer = null)
            => source.Distinct(new EqualityComparerDelegateWrapper<T>(comparer, hashCodeComparer));

        /// <summary>Groups values by key and projects each group using the supplied key equality delegate.</summary>
        public static IEnumerable<TResult> GroupBy<TSource, TKey, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TKey, IEnumerable<TSource>, TResult> resultSelector,
            Func<TKey?, TKey?, bool> comparer, Func<TKey, int>? hashCodeComparer = null)
            => source.GroupBy(keySelector, resultSelector, new EqualityComparerDelegateWrapper<TKey>(comparer, hashCodeComparer));

        /// <summary>Groups projected elements by key and projects each group using a key equality delegate.</summary>
        public static IEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            Func<TKey, IEnumerable<TElement>, TResult> resultSelector,
            Func<TKey?, TKey?, bool> comparer, Func<TKey, int>? hashCodeComparer = null)
            => source.GroupBy(keySelector, elementSelector, resultSelector, new EqualityComparerDelegateWrapper<TKey>(comparer, hashCodeComparer));

        /// <summary>Groups values by keys compared with the supplied equality delegate.</summary>
        public static IEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector, 
            Func<TKey?, TKey?, bool> comparer, Func<TKey, int>? hashCodeComparer = null)
            => source.GroupBy(keySelector, new EqualityComparerDelegateWrapper<TKey>(comparer, hashCodeComparer));

        /// <summary>Groups projected elements by keys compared with the supplied equality delegate.</summary>
        public static IEnumerable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(
            this IEnumerable<TSource> source, 
            Func<TSource, TKey> keySelector, 
            Func<TSource, TElement> elementSelector,
            Func<TKey?, TKey?, bool> comparer, Func<TKey, int>? hashCodeComparer = null)
            => source.GroupBy(keySelector, elementSelector, new EqualityComparerDelegateWrapper<TKey>(comparer, hashCodeComparer));

        /// <summary>Joins each outer value to a group of inner values using a key equality delegate.</summary>
        public static IEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(
            this IEnumerable<TOuter> outer,
            IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector,
            Func<TOuter, IEnumerable<TInner>, TResult> resultSelector,
            Func<TKey?, TKey?, bool> comparer, Func<TKey, int>? hashCodeComparer = null)
            => outer.GroupJoin(inner, outerKeySelector, innerKeySelector, resultSelector, new EqualityComparerDelegateWrapper<TKey>(comparer, hashCodeComparer));

        /// <summary>Joins matching values from two sequences using a key equality delegate.</summary>
        public static IEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(
            this IEnumerable<TOuter> outer,
            IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector,
            Func<TKey?, TKey?, bool> comparer, Func<TKey, int>? hashCodeComparer = null)
            => outer.Join(inner, outerKeySelector, innerKeySelector, resultSelector, new EqualityComparerDelegateWrapper<TKey>(comparer, hashCodeComparer));

        /// <summary>Creates a dictionary of projected keys and values using a key equality delegate.</summary>
        public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            Func<TKey?, TKey?, bool> comparer, Func<TKey, int>? hashCodeComparer = null)
            where TKey : notnull => source.ToDictionary(keySelector, elementSelector, new EqualityComparerDelegateWrapper<TKey>(comparer, hashCodeComparer));

        /// <summary>Creates a lookup of projected elements using a key equality delegate.</summary>
        public static ILookup<TKey, TElement> ToLookup<TSource, TKey, TElement>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            Func<TKey?, TKey?, bool> comparer, Func<TKey, int>? hashCodeComparer = null)
            => source.ToLookup(keySelector, elementSelector, new EqualityComparerDelegateWrapper<TKey>(comparer, hashCodeComparer));

        /// <summary>Creates a lookup of source values using a key equality delegate.</summary>
        public static ILookup<TKey, TSource> ToLookup<TSource, TKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TKey?, TKey?, bool> comparer, Func<TKey, int>? hashCodeComparer = null)
            => source.ToLookup(keySelector, new EqualityComparerDelegateWrapper<TKey>(comparer, hashCodeComparer));

        /// <summary>Returns distinct values from both sequences using the supplied equality delegate.</summary>
        public static IEnumerable<TSource> Union<TSource>(
            this IEnumerable<TSource> first,
            IEnumerable<TSource> second,
            Func<TSource?, TSource?, bool> comparer, Func<TSource, int>? hashCodeComparer = null)
            => first.Union(second, new EqualityComparerDelegateWrapper<TSource>(comparer, hashCodeComparer));
    }
}
