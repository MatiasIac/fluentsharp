using System;
using System.Collections.Generic;
using System.Linq;

namespace FunctionalSharp.Linq
{
    public static class LinqExtenders
    {

        public static IEnumerable<T> Intersect<T>(this IEnumerable<T> source, IEnumerable<T> second, Func<T, T, bool> comparer)
            => source.Intersect(second, new EqualityComparerDelegateWrapper<T>(comparer));
        
        public static IEnumerable<T> Except<T>(this IEnumerable<T> source, IEnumerable<T> second, Func<T, T, bool> comparer)
            => source.Except(second, new EqualityComparerDelegateWrapper<T>(comparer));

        public static bool Contains<T>(this IEnumerable<T> source, T value, Func<T, T, bool> comparer)
            => source.Contains(value, new EqualityComparerDelegateWrapper<T>(comparer));

        public static IEnumerable<T> Distinct<T>(this IEnumerable<T> source, Func<T, T, bool> comparer)
            => source.Distinct(new EqualityComparerDelegateWrapper<T>(comparer));

        public static IEnumerable<TResult> GroupBy<TSource, TKey, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TKey, IEnumerable<TSource>, TResult> resultSelector,
            Func<TKey, TKey, bool> comparer)
            => source.GroupBy(keySelector, resultSelector, new EqualityComparerDelegateWrapper<TKey>(comparer));

        public static IEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            Func<TKey, IEnumerable<TElement>, TResult> resultSelector,
            Func<TKey, TKey, bool> comparer)
            => source.GroupBy(keySelector, elementSelector, resultSelector, new EqualityComparerDelegateWrapper<TKey>(comparer));

        public static IEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector, 
            Func<TKey, TKey, bool> comparer)
            => source.GroupBy(keySelector, new EqualityComparerDelegateWrapper<TKey>(comparer));

        public static IEnumerable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(
            this IEnumerable<TSource> source, 
            Func<TSource, TKey> keySelector, 
            Func<TSource, TElement> elementSelector,
            Func<TKey, TKey, bool> comparer)
            => source.GroupBy(keySelector, elementSelector, new EqualityComparerDelegateWrapper<TKey>(comparer));

        public static IEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(
            this IEnumerable<TOuter> outer,
            IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector,
            Func<TOuter, IEnumerable<TInner>, TResult> resultSelector,
            Func<TKey, TKey, bool> comparer)
            => outer.GroupJoin(inner, outerKeySelector, innerKeySelector, resultSelector, new EqualityComparerDelegateWrapper<TKey>(comparer));

        public static IEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(
            this IEnumerable<TOuter> outer,
            IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector,
            Func<TKey, TKey, bool> comparer)
            => outer.Join(inner, outerKeySelector, innerKeySelector, resultSelector, new EqualityComparerDelegateWrapper<TKey>(comparer));

        public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            Func<TKey, TKey, bool> comparer)
            => source.ToDictionary(keySelector, elementSelector, new EqualityComparerDelegateWrapper<TKey>(comparer));

        public static ILookup<TKey, TElement> ToLookup<TSource, TKey, TElement>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            Func<TKey, TKey, bool> comparer)
            => source.ToLookup(keySelector, elementSelector, new EqualityComparerDelegateWrapper<TKey>(comparer));

        public static ILookup<TKey, TSource> ToLookup<TSource, TKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TKey, TKey, bool> comparer)
            => source.ToLookup(keySelector, new EqualityComparerDelegateWrapper<TKey>(comparer));

        public static IEnumerable<TSource> Union<TSource>(
            this IEnumerable<TSource> first,
            IEnumerable<TSource> second,
            Func<TSource, TSource, bool> comparer)
            => first.Union(second, new EqualityComparerDelegateWrapper<TSource>(comparer));
    }
}