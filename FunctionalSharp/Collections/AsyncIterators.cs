using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FunctionalSharp.Collections
{
    /// <summary>Provides task-returning wrappers for synchronous collection operations.</summary>
    /// <remarks>
    /// These methods accept synchronous delegates, not asynchronous callbacks.
    /// Iteration uses Task.Run; ThenAsync and AlterAsync execute their delegates synchronously.
    /// </remarks>
    public static class AsyncIterators
    {

        /// <summary>
        /// Allows to handle a collection across all its elements applying a particular action
        /// </summary>
        /// <typeparam name="T">Any</typeparam>
        /// <param name="collection"></param>
        /// <param name="action"></param>
        /// <returns>The input IEnumerable&lt;<typeparamref name="T"/>&gt; collection used in the operation</returns>
        public static async Task<IEnumerable<T>> ThenAsync<T>(
            this IEnumerable<T> collection, 
            Action<IEnumerable<T>> action) => await Task.FromResult(collection.Then(action));


        /// <summary>Executes a synchronous sequence transformation and wraps its result in a task.</summary>
        /// <typeparam name="T">The sequence element type.</typeparam>
        /// <param name="collection">The sequence to transform.</param>
        /// <param name="action">A synchronous delegate producing the result sequence.</param>
        /// <returns>A task containing the delegate's result.</returns>
        public static async Task<IEnumerable<T>> AlterAsync<T>(this IEnumerable<T> collection, Func<IEnumerable<T>, IEnumerable<T>> action)
            => await Task.FromResult(collection.Alter(action));

        /// <summary>
        /// Iterates across the collection passing the current item to the defined action
        /// </summary>
        /// <typeparam name="T">A type supported by the iterable collection</typeparam>
        /// <param name="collection">Iterable collection</param>
        /// <param name="action">Action to be executed for each collection item</param>
        public static async Task ForEveryAsync<T>(this IEnumerable<T> collection, Action<T> action)
            => await Task.Run(() => collection.ForEvery(action));

        /// <summary>
        /// Iterate the IEnumerable&lt;<typeparamref name="T"/>&gt; collection and applies the expected condition
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="condition">Condition that will be applied during the iteration. Func&lt;<typeparamref name="T"/>, int index, bool output&gt;</param>
        /// <param name="action">Action to be executed for each collection item</param>
        public static async Task ForAsync<T>(this IEnumerable<T> collection, Func<T, int, bool> condition, Action<T> action)
            => await Task.Run(() => collection.For(condition, action));

        /// <summary>
        /// Iterate across the collection applying the defined condition without passing the current collection index
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="condition">Condition that will be applied during the iteration. Func&lt;<typeparamref name="T"/>, bool output&gt;</param>
        /// <param name="action">Action to be executed for each collection item</param>
        public static async Task ForAsync<T>(this IEnumerable<T> collection, Func<T, bool> condition, Action<T> action)
            => await Task.Run(() => collection.For(condition, action));

        /// <summary>
        /// Iterate across the collection and stops when the action returns false
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">Iterable collection</param>
        /// <param name="action">Action to be executed for each collection item</param>
        public static async Task ForAsync<T>(this IEnumerable<T> collection, Func<T, bool> action)
            => await Task.Run(() => collection.For(action));
    }
}
