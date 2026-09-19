using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FunctionalSharp.Collections
{
    /// <summary>Provides awaited actions, transformations, and sequential iteration over sequences.</summary>
    /// <remarks>
    /// Callbacks must return tasks. No work is scheduled through Task.Run. Cancellation is checked
    /// before enumeration, between items, and after callbacks, and is passed to token-aware delegates.
    /// Running callbacks are awaited even if cancellation is requested; cancellation is cooperative.
    /// Iterators are disposed on completion, early stopping, failure, and cancellation.
    /// Null arguments are rejected synchronously; execution failures are reported through the task.
    /// </remarks>
    public static class AsyncIterators
    {
        /// <summary>Awaits an action on the whole sequence and returns the original sequence without enumerating it.</summary>
        public static Task<IEnumerable<T>> ThenAsync<T>(this IEnumerable<T> collection,
            Func<IEnumerable<T>, Task> action, CancellationToken cancellationToken = default)
        {
            ValidateArguments(collection, action);
            return ThenCoreAsync(collection, (items, token) => action(items), cancellationToken);
        }

        /// <summary>Awaits a token-aware action on the whole sequence and returns the original sequence.</summary>
        public static Task<IEnumerable<T>> ThenAsync<T>(this IEnumerable<T> collection,
            Func<IEnumerable<T>, CancellationToken, Task> action, CancellationToken cancellationToken = default)
        {
            ValidateArguments(collection, action);
            return ThenCoreAsync(collection, action, cancellationToken);
        }

        /// <summary>Awaits a sequence transformation and returns its result without enumerating either sequence.</summary>
        public static Task<IEnumerable<T>> AlterAsync<T>(this IEnumerable<T> collection,
            Func<IEnumerable<T>, Task<IEnumerable<T>>> action, CancellationToken cancellationToken = default)
        {
            ValidateArguments(collection, action);
            return AlterCoreAsync(collection, (items, token) => action(items), cancellationToken);
        }

        /// <summary>Awaits a token-aware sequence transformation and returns its result.</summary>
        public static Task<IEnumerable<T>> AlterAsync<T>(this IEnumerable<T> collection,
            Func<IEnumerable<T>, CancellationToken, Task<IEnumerable<T>>> action, CancellationToken cancellationToken = default)
        {
            ValidateArguments(collection, action);
            return AlterCoreAsync(collection, action, cancellationToken);
        }

        /// <summary>Awaits each item action sequentially in enumeration order.</summary>
        public static Task ForEveryAsync<T>(this IEnumerable<T> collection,
            Func<T, Task> action, CancellationToken cancellationToken = default)
        {
            ValidateArguments(collection, action);
            return ForCoreAsync(collection, null, (item, token) => action(item), cancellationToken);
        }

        /// <summary>Awaits each token-aware item action sequentially in enumeration order.</summary>
        public static Task ForEveryAsync<T>(this IEnumerable<T> collection,
            Func<T, CancellationToken, Task> action, CancellationToken cancellationToken = default)
        {
            ValidateArguments(collection, action);
            return ForCoreAsync(collection, null, action, cancellationToken);
        }

        /// <summary>Awaits item actions until the synchronous indexed condition first returns false.</summary>
        /// <remarks>The condition is evaluated before the action; indexes start at zero.</remarks>
        public static Task ForAsync<T>(this IEnumerable<T> collection, Func<T, int, bool> condition,
            Func<T, Task> action, CancellationToken cancellationToken = default)
        {
            ValidateArguments(collection, action);
            if (condition == null) throw new ArgumentNullException(nameof(condition));
            return ForCoreAsync(collection, condition, (item, token) => action(item), cancellationToken);
        }

        /// <summary>Awaits token-aware item actions until the synchronous indexed condition first returns false.</summary>
        public static Task ForAsync<T>(this IEnumerable<T> collection, Func<T, int, bool> condition,
            Func<T, CancellationToken, Task> action, CancellationToken cancellationToken = default)
        {
            ValidateArguments(collection, action);
            if (condition == null) throw new ArgumentNullException(nameof(condition));
            return ForCoreAsync(collection, condition, action, cancellationToken);
        }

        /// <summary>Awaits item actions until the synchronous condition first returns false.</summary>
        public static Task ForAsync<T>(this IEnumerable<T> collection, Func<T, bool> condition,
            Func<T, Task> action, CancellationToken cancellationToken = default)
        {
            ValidateArguments(collection, action);
            if (condition == null) throw new ArgumentNullException(nameof(condition));
            return ForCoreAsync(collection, (item, index) => condition(item), (item, token) => action(item), cancellationToken);
        }

        /// <summary>Awaits token-aware item actions until the synchronous condition first returns false.</summary>
        public static Task ForAsync<T>(this IEnumerable<T> collection, Func<T, bool> condition,
            Func<T, CancellationToken, Task> action, CancellationToken cancellationToken = default)
        {
            ValidateArguments(collection, action);
            if (condition == null) throw new ArgumentNullException(nameof(condition));
            return ForCoreAsync(collection, (item, index) => condition(item), action, cancellationToken);
        }

        /// <summary>Awaits each item callback and stops when its result is false.</summary>
        public static Task ForAsync<T>(this IEnumerable<T> collection,
            Func<T, Task<bool>> action, CancellationToken cancellationToken = default)
        {
            ValidateArguments(collection, action);
            return ForUntilCoreAsync(collection, (item, token) => action(item), cancellationToken);
        }

        /// <summary>Awaits each token-aware item callback and stops when its result is false.</summary>
        public static Task ForAsync<T>(this IEnumerable<T> collection,
            Func<T, CancellationToken, Task<bool>> action, CancellationToken cancellationToken = default)
        {
            ValidateArguments(collection, action);
            return ForUntilCoreAsync(collection, action, cancellationToken);
        }

        private static void ValidateArguments<T>(IEnumerable<T> collection, Delegate action)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (action == null) throw new ArgumentNullException(nameof(action));
        }

        private static async Task<IEnumerable<T>> ThenCoreAsync<T>(IEnumerable<T> collection,
            Func<IEnumerable<T>, CancellationToken, Task> action, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var task = action(collection, cancellationToken)
                ?? throw new InvalidOperationException("The action returned a null task.");
            await task.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return collection;
        }

        private static async Task<IEnumerable<T>> AlterCoreAsync<T>(IEnumerable<T> collection,
            Func<IEnumerable<T>, CancellationToken, Task<IEnumerable<T>>> action, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var task = action(collection, cancellationToken)
                ?? throw new InvalidOperationException("The action returned a null task.");
            var result = await task.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return result ?? throw new InvalidOperationException("The action returned a null sequence.");
        }

        private static async Task ForCoreAsync<T>(IEnumerable<T> collection, Func<T, int, bool>? condition,
            Func<T, CancellationToken, Task> action, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using (var iterator = collection.GetEnumerator())
            {
                var index = 0;
                while (MoveNext(iterator, cancellationToken))
                {
                    var item = iterator.Current;
                    cancellationToken.ThrowIfCancellationRequested();
                    var shouldExecute = condition == null || condition(item, index);
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!shouldExecute) return;

                    var task = action(item, cancellationToken)
                        ?? throw new InvalidOperationException("The action returned a null task.");
                    await task.ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();
                    index++;
                }
            }
        }

        private static async Task ForUntilCoreAsync<T>(IEnumerable<T> collection,
            Func<T, CancellationToken, Task<bool>> action, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using (var iterator = collection.GetEnumerator())
            {
                while (MoveNext(iterator, cancellationToken))
                {
                    var item = iterator.Current;
                    cancellationToken.ThrowIfCancellationRequested();
                    var task = action(item, cancellationToken)
                        ?? throw new InvalidOperationException("The action returned a null task.");
                    var shouldContinue = await task.ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!shouldContinue) return;
                }
            }
        }

        private static bool MoveNext<T>(IEnumerator<T> iterator, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var hasNext = iterator.MoveNext();
            cancellationToken.ThrowIfCancellationRequested();
            return hasNext;
        }
    }
}
