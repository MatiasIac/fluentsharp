using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FunctionalSharp.Collections.Tests
{
    [TestClass]
    public class AsyncExecutionTests
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

        [TestMethod]
        [DataRow("each")]
        [DataRow("condition")]
        [DataRow("indexed")]
        [DataRow("until")]
        public async Task Iteration_AwaitsEachCallback_BeforeReadingTheNextItem(string operationKind)
        {
            var entered = Gate();
            var release = Gate();
            var started = new List<int>();
            var completed = new List<int>();
            var source = new TrackedSequence(1, 2, 3);
            var operation = RunIteration(operationKind, source, async item =>
            {
                started.Add(item);
                if (item == 1)
                {
                    entered.SetResult(true);
                    await release.Task;
                }
                completed.Add(item);
            });

            try
            {
                await entered.Task.WaitAsync(Timeout);
                Assert.IsFalse(operation.IsCompleted);
                CollectionAssert.AreEqual(new[] { 1 }, started);
                Assert.AreEqual(0, completed.Count);
                Assert.AreEqual(1, source.ItemsRead);
            }
            finally
            {
                release.TrySetResult(true);
                await operation.WaitAsync(Timeout);
            }

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, completed);
            Assert.AreEqual(1, source.EnumerationCount);
            Assert.AreEqual(1, source.DisposeCount);
        }

        [TestMethod]
        [DataRow("each", false)]
        [DataRow("each", true)]
        [DataRow("condition", false)]
        [DataRow("condition", true)]
        [DataRow("indexed", false)]
        [DataRow("indexed", true)]
        [DataRow("until", false)]
        [DataRow("until", true)]
        public async Task CallbackFailure_FaultsTheTask_StopsIteration_AndDisposes(string operationKind, bool afterAwait)
        {
            var failure = new InvalidOperationException("Callback failed.");
            var entered = Gate();
            var release = Gate();
            var source = new TrackedSequence(1, 2, 3);
            var completed = new List<int>();

            async Task FailAfterAwait()
            {
                entered.SetResult(true);
                await release.Task;
                throw failure;
            }

            Task Process(int item)
            {
                if (item == 2)
                {
                    if (afterAwait) return FailAfterAwait();
                    throw failure;
                }
                completed.Add(item);
                return Task.CompletedTask;
            }

            // Synchronous delegate exceptions must also be captured by the returned task.
            var operation = RunIteration(operationKind, source, Process);
            try
            {
                if (afterAwait)
                {
                    await entered.Task.WaitAsync(Timeout);
                    Assert.IsFalse(operation.IsCompleted);
                }
            }
            finally
            {
                release.TrySetResult(true);
            }

            var error = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => operation.WaitAsync(Timeout));
            Assert.AreSame(failure, error);
            CollectionAssert.AreEqual(new[] { 1 }, completed);
            Assert.AreEqual(2, source.ItemsRead);
            Assert.AreEqual(1, source.DisposeCount);
        }

        [TestMethod]
        [DataRow("then")]
        [DataRow("alter")]
        [DataRow("each")]
        [DataRow("condition")]
        [DataRow("indexed")]
        [DataRow("until")]
        public async Task PreCancelledToken_SkipsEnumerationAndCallbacks(string operationKind)
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var source = new TrackedSequence(1, 2);
            var calls = 0;
            var operation = RunTokenAware(operationKind, source,
                (item, token) => { calls++; return Task.CompletedTask; }, cancellation.Token);

            var error = await Assert.ThrowsAsync<OperationCanceledException>(() => operation);

            Assert.AreEqual(cancellation.Token, error.CancellationToken);
            Assert.IsTrue(operation.IsCanceled);
            Assert.AreEqual(0, calls);
            Assert.AreEqual(0, source.EnumerationCount);
        }

        [TestMethod]
        [DataRow("then")]
        [DataRow("alter")]
        [DataRow("each")]
        [DataRow("condition")]
        [DataRow("indexed")]
        [DataRow("until")]
        public async Task Cancellation_PassesTheToken_AndAwaitsRunningCallbacks(string operationKind)
        {
            using var cancellation = new CancellationTokenSource();
            var source = new TrackedSequence(1, 2);
            var entered = Gate();
            var release = Gate();
            var calls = 0;
            var receivedToken = CancellationToken.None;
            var operation = RunTokenAware(operationKind, source, async (item, token) =>
            {
                calls++;
                receivedToken = token;
                entered.SetResult(true);
                // Deliberately ignore cancellation until this work finishes.
                await release.Task;
            }, cancellation.Token);

            try
            {
                await entered.Task.WaitAsync(Timeout);
                cancellation.Cancel();
                Assert.IsFalse(operation.IsCompleted, "Cancellation must not leave a callback running unawaited.");
            }
            finally
            {
                release.TrySetResult(true);
            }

            var error = await Assert.ThrowsAsync<OperationCanceledException>(() => operation.WaitAsync(Timeout));
            Assert.AreEqual(cancellation.Token, receivedToken);
            Assert.AreEqual(cancellation.Token, error.CancellationToken);
            Assert.IsTrue(operation.IsCanceled);
            Assert.AreEqual(1, calls);
            var iterates = operationKind != "then" && operationKind != "alter";
            Assert.AreEqual(iterates ? 1 : 0, source.ItemsRead);
            Assert.AreEqual(iterates ? 1 : 0, source.DisposeCount);
        }

        [TestMethod]
        public async Task CancellationInsideCondition_PreventsTheAction_EvenWhenConditionIsFalse()
        {
            using var cancellation = new CancellationTokenSource();
            var source = new TrackedSequence(1, 2);
            var calls = 0;
            var operation = source.ForAsync(
                item => { cancellation.Cancel(); return false; },
                item => { calls++; return Task.CompletedTask; }, cancellation.Token);

            await Assert.ThrowsAsync<OperationCanceledException>(() => operation);
            Assert.AreEqual(0, calls);
            Assert.AreEqual(1, source.ItemsRead);
            Assert.AreEqual(1, source.DisposeCount);
        }

        [TestMethod]
        public async Task CancellationDuringMoveNext_PreventsConditionAndAction()
        {
            using var cancellation = new CancellationTokenSource();
            var source = new TrackedSequence(1, 2) { BeforeYield = item => cancellation.Cancel() };
            var conditionCalls = 0;
            var actionCalls = 0;
            var operation = source.ForAsync(
                (item, index) => { conditionCalls++; return true; },
                item => { actionCalls++; return Task.CompletedTask; }, cancellation.Token);

            await Assert.ThrowsAsync<OperationCanceledException>(() => operation);
            Assert.AreEqual(0, conditionCalls);
            Assert.AreEqual(0, actionCalls);
            Assert.AreEqual(1, source.DisposeCount);
        }

        [TestMethod]
        public async Task CallbackCancellation_PropagatesItsOriginalToken()
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var source = new TrackedSequence(1, 2);
            var operation = source.ForEveryAsync(item => Task.FromCanceled(cancellation.Token));

            var error = await Assert.ThrowsAsync<OperationCanceledException>(() => operation);

            Assert.AreEqual(cancellation.Token, error.CancellationToken);
            Assert.IsTrue(operation.IsCanceled);
            Assert.AreEqual(1, source.ItemsRead);
            Assert.AreEqual(1, source.DisposeCount);
        }

        [TestMethod]
        [DataRow("condition")]
        [DataRow("indexed")]
        [DataRow("until")]
        public async Task FalseResult_StopsAtTheFirstFalseResult_AndDisposes(string operationKind)
        {
            var source = new TrackedSequence(1, 2, 3, 4);
            var visited = new List<int>();
            var indexes = new List<int>();
            Task Process(int item) { visited.Add(item); return Task.CompletedTask; }

            switch (operationKind)
            {
                case "condition":
                    await source.ForAsync(item => item < 3, Process);
                    break;
                case "indexed":
                    await source.ForAsync((item, index) => { indexes.Add(index); return index < 2; }, Process);
                    CollectionAssert.AreEqual(new[] { 0, 1, 2 }, indexes);
                    break;
                default:
                    await source.ForAsync(item => { visited.Add(item); return Task.FromResult(item < 2); });
                    break;
            }

            CollectionAssert.AreEqual(new[] { 1, 2 }, visited);
            Assert.AreEqual(operationKind == "until" ? 2 : 3, source.ItemsRead);
            Assert.AreEqual(1, source.DisposeCount);
        }

        [TestMethod]
        [DataRow("each")]
        [DataRow("condition")]
        [DataRow("indexed")]
        [DataRow("until")]
        public async Task EmptySequence_DoesNotInvokeItemCallbacks(string operationKind)
        {
            var source = new TrackedSequence();
            var calls = 0;

            await RunIteration(operationKind, source, item => { calls++; return Task.CompletedTask; });

            Assert.AreEqual(0, calls);
            Assert.AreEqual(1, source.EnumerationCount);
            Assert.AreEqual(1, source.DisposeCount);
        }

        [TestMethod]
        public async Task ThenAndAlter_DoNotEnumerate_AndReturnTheCorrectSequence()
        {
            var source = new TrackedSequence(1, 2);
            var result = new TrackedSequence(3, 4);
            var transformation = new TaskCompletionSource<IEnumerable<int>>(TaskCreationOptions.RunContinuationsAsynchronously);
            IEnumerable<int> received = null;
            Task<IEnumerable<int>> Transform(IEnumerable<int> items) { received = items; return transformation.Task; }

            var operation = source.AlterAsync(Transform);
            try
            {
                Assert.IsFalse(operation.IsCompleted);
                Assert.AreSame(source, received);
            }
            finally
            {
                transformation.TrySetResult(result);
            }

            Assert.AreSame(result, await operation.WaitAsync(Timeout));
            Assert.AreSame(source, await source.ThenAsync(items => Task.CompletedTask));
            Assert.AreEqual(0, source.EnumerationCount);
            Assert.AreEqual(0, result.EnumerationCount);
        }

        [TestMethod]
        [DataRow("then")]
        [DataRow("alter")]
        public async Task SequenceCallbackFailure_PropagatesTheOriginalException(string operationKind)
        {
            var source = new TrackedSequence(1, 2);
            var failure = new InvalidOperationException("Sequence callback failed.");
            var callback = new TaskCompletionSource<IEnumerable<int>>(TaskCreationOptions.RunContinuationsAsynchronously);
            var operation = operationKind == "then"
                ? source.ThenAsync(items => callback.Task)
                : source.AlterAsync(items => callback.Task);

            try
            {
                Assert.IsFalse(operation.IsCompleted);
            }
            finally
            {
                callback.TrySetException(failure);
            }

            var error = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => operation.WaitAsync(Timeout));
            Assert.AreSame(failure, error);
            Assert.IsTrue(operation.IsFaulted);
            Assert.AreEqual(0, source.EnumerationCount);
        }

        [TestMethod]
        [DataRow("each")]
        [DataRow("condition")]
        [DataRow("indexed")]
        [DataRow("until")]
        public async Task EnumerationFailure_FaultsTheTask_AndDisposes(string operationKind)
        {
            var failure = new InvalidOperationException("Enumeration failed.");
            var source = new TrackedSequence(1, 2, 3)
            {
                BeforeYield = item => { if (item == 2) throw failure; }
            };
            var calls = 0;
            var operation = RunIteration(operationKind, source, item => { calls++; return Task.CompletedTask; });

            var error = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => operation);

            Assert.AreSame(failure, error);
            Assert.AreEqual(1, calls);
            Assert.AreEqual(1, source.DisposeCount);
        }

        [TestMethod]
        public async Task ConditionFailure_FaultsTheTask_AndDisposes()
        {
            var failure = new InvalidOperationException("Condition failed.");
            var source = new TrackedSequence(1, 2);
            var calls = 0;
            var operation = source.ForAsync((item, index) => throw failure,
                item => { calls++; return Task.CompletedTask; });

            var error = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => operation);

            Assert.AreSame(failure, error);
            Assert.AreEqual(0, calls);
            Assert.AreEqual(1, source.DisposeCount);
        }

        [TestMethod]
        public async Task TaskReturningMethodGroups_BindAcrossAllOverloads()
        {
            var source = new[] { 1, 2 };
            var observed = 0;
            var processed = 0;
            Task Observe(IEnumerable<int> items) { observed++; return Task.CompletedTask; }
            Task ObserveWithToken(IEnumerable<int> items, CancellationToken token) => Observe(items);
            Task<IEnumerable<int>> Transform(IEnumerable<int> items) => Task.FromResult(items);
            Task<IEnumerable<int>> TransformWithToken(IEnumerable<int> items, CancellationToken token) => Transform(items);
            Task Process(int item) { processed++; return Task.CompletedTask; }
            Task ProcessWithToken(int item, CancellationToken token) => Process(item);
            Task<bool> Continue(int item) { processed++; return Task.FromResult(true); }
            Task<bool> ContinueWithToken(int item, CancellationToken token) => Continue(item);

            Assert.AreSame(source, await source.ThenAsync(Observe));
            Assert.AreSame(source, await source.ThenAsync(ObserveWithToken));
            Assert.AreSame(source, await source.AlterAsync(Transform));
            Assert.AreSame(source, await source.AlterAsync(TransformWithToken));
            await source.ForEveryAsync(Process);
            await source.ForEveryAsync(ProcessWithToken);
            await source.ForAsync(item => true, Process);
            await source.ForAsync(item => true, ProcessWithToken);
            await source.ForAsync((item, index) => true, Process);
            await source.ForAsync((item, index) => true, ProcessWithToken);
            await source.ForAsync(Continue);
            await source.ForAsync(ContinueWithToken);

            Assert.AreEqual(2, observed);
            Assert.AreEqual(16, processed);
        }

        [TestMethod]
        public void NullDelegates_AreRejectedSynchronously_BeforeEnumeration()
        {
            var source = new TrackedSequence(1);
            var calls = new Func<Task>[]
            {
                () => source.ThenAsync((Func<IEnumerable<int>, Task>)null),
                () => source.ThenAsync((Func<IEnumerable<int>, CancellationToken, Task>)null),
                () => source.AlterAsync((Func<IEnumerable<int>, Task<IEnumerable<int>>>)null),
                () => source.AlterAsync((Func<IEnumerable<int>, CancellationToken, Task<IEnumerable<int>>>)null),
                () => source.ForEveryAsync((Func<int, Task>)null),
                () => source.ForEveryAsync((Func<int, CancellationToken, Task>)null),
                () => source.ForAsync(item => true, (Func<int, Task>)null),
                () => source.ForAsync(item => true, (Func<int, CancellationToken, Task>)null),
                () => source.ForAsync((item, index) => true, (Func<int, Task>)null),
                () => source.ForAsync((item, index) => true, (Func<int, CancellationToken, Task>)null),
                () => source.ForAsync((Func<int, Task<bool>>)null),
                () => source.ForAsync((Func<int, CancellationToken, Task<bool>>)null)
            };

            foreach (var call in calls)
            {
                var error = Assert.ThrowsExactly<ArgumentNullException>(() => { _ = call(); });
                Assert.AreEqual("action", error.ParamName);
            }
            Assert.AreEqual(0, source.EnumerationCount);
        }

        [TestMethod]
        public void NullConditions_AreRejectedSynchronously()
        {
            var source = new[] { 1 };
            var calls = new Func<Task>[]
            {
                () => source.ForAsync((Func<int, bool>)null, item => Task.CompletedTask),
                () => source.ForAsync((Func<int, bool>)null, (item, token) => Task.CompletedTask),
                () => source.ForAsync((Func<int, int, bool>)null, item => Task.CompletedTask),
                () => source.ForAsync((Func<int, int, bool>)null, (item, token) => Task.CompletedTask)
            };
            foreach (var call in calls)
            {
                var error = Assert.ThrowsExactly<ArgumentNullException>(() => { _ = call(); });
                Assert.AreEqual("condition", error.ParamName);
            }
        }

        [TestMethod]
        [DataRow("then")]
        [DataRow("alter")]
        [DataRow("each")]
        [DataRow("condition")]
        [DataRow("indexed")]
        [DataRow("until")]
        public void NullCollection_IsRejectedSynchronously(string operationKind)
        {
            var error = Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _ = RunTokenAware(operationKind, null, (item, token) => Task.CompletedTask, CancellationToken.None);
            });

            Assert.AreEqual("collection", error.ParamName);
        }

        [TestMethod]
        public async Task NullTaskOrTransformationResult_ReportsAClearFailure()
        {
            var source = new[] { 1 };
            var calls = new Func<Task>[]
            {
                () => source.ThenAsync(items => (Task)null),
                () => source.AlterAsync(items => (Task<IEnumerable<int>>)null),
                () => source.ForEveryAsync(item => (Task)null),
                () => source.ForAsync(item => (Task<bool>)null)
            };
            foreach (var call in calls)
            {
                var error = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => call());
                StringAssert.Contains(error.Message, "null task");
            }

            var resultError = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
                source.AlterAsync(items => Task.FromResult<IEnumerable<int>>(null)));
            StringAssert.Contains(resultError.Message, "null sequence");
        }

        private static TaskCompletionSource<bool> Gate() => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        private static Task RunIteration(string kind, IEnumerable<int> source, Func<int, Task> action)
        {
            switch (kind)
            {
                case "each": return source.ForEveryAsync(action);
                case "condition": return source.ForAsync(item => true, action);
                case "indexed": return source.ForAsync((item, index) => true, action);
                case "until": return source.ForAsync(async item => { await action(item); return true; });
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        private static Task RunTokenAware(string kind, IEnumerable<int> source,
            Func<int, CancellationToken, Task> action, CancellationToken cancellationToken)
        {
            switch (kind)
            {
                case "then": return source.ThenAsync((items, token) => action(0, token), cancellationToken);
                case "alter": return source.AlterAsync(async (items, token) => { await action(0, token); return items; }, cancellationToken);
                case "each": return source.ForEveryAsync(action, cancellationToken);
                case "condition": return source.ForAsync(item => true, action, cancellationToken);
                case "indexed": return source.ForAsync((item, index) => true, action, cancellationToken);
                case "until": return source.ForAsync(async (item, token) => { await action(item, token); return true; }, cancellationToken);
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        private sealed class TrackedSequence : IEnumerable<int>
        {
            private readonly int[] values;
            public int EnumerationCount { get; private set; }
            public int ItemsRead { get; private set; }
            public int DisposeCount { get; private set; }
            public Action<int> BeforeYield { get; set; }

            public TrackedSequence(params int[] values) => this.values = values;

            public IEnumerator<int> GetEnumerator()
            {
                EnumerationCount++;
                if (EnumerationCount > 1) throw new InvalidOperationException("This sequence can only be enumerated once.");
                return Enumerate().GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private IEnumerable<int> Enumerate()
            {
                try
                {
                    foreach (var value in values)
                    {
                        BeforeYield?.Invoke(value);
                        ItemsRead++;
                        yield return value;
                    }
                }
                finally
                {
                    DisposeCount++;
                }
            }
        }
    }
}
