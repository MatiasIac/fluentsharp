using System;
using System.Collections.Generic;

namespace FunctionalSharp.Collections
{
    public static class Iterators
    {

        /// <summary>
        /// Allows to iterate a collection across all its elements applying a particular action
        /// </summary>
        /// <typeparam name="T">Any</typeparam>
        /// <param name="collection"></param>
        /// <param name="action"></param>
        /// <returns>The input IEnumerable&lt;<typeparamref name="T"/>&gt; collection used in the operation</returns>
        public static IEnumerable<T> Then<T>(this IEnumerable<T> collection, Action<IEnumerable<T>> action)
        {
            action(collection);
            return collection;
        }

        public static void ForEachItem<T>(this IEnumerable<T> collection, Action<T> action)
        {
            foreach (var item in collection)
            {
                action(item);
            }
        }

        /// <summary>
        /// Iterate the IEnumerable&lt;<typeparamref name="T"/>&gt; collection and applies the expected condition
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="condition">Condition that will be applied during the iteration. Func&lt;<typeparamref name="T"/>, int index, bool output&gt;</param>
        /// <param name="action"></param>
        /// <returns>The index when the iteration stopped. -1 when iteration did not started.</returns>
        public static int IterateUntil<T>(this IEnumerable<T> collection, Func<T, int, bool> condition, Action<T> action)
        {
            var index = -1;

            foreach (var item in collection)
            {
                if (!condition(item, index)) break;
                index++;
                action(item);
            }

            return index;
        }

    }
}