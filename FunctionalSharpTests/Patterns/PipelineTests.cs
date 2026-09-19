#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FunctionalSharp.Patterns.Tests;

[TestClass]
public class PipelineTests
{
    [TestMethod]
    public void DefinitionsAreImmutable_AndEachRunUsesItsSuppliedPayload()
    {
        var empty = Pipeline<int>.Create();
        var increment = empty.Then(value => value + 1);
        var doubled = increment.Then(value => value * 2);
        Assert.AreEqual(3, empty.Run(3).Payload);
        Assert.AreEqual(4, increment.Run(3).Payload);
        Assert.AreEqual(8, doubled.Run(3).Payload);
        Assert.AreEqual(12, doubled.Run(5).Payload);
        Assert.AreEqual(8, doubled.Run(3).Payload);
        Assert.AreEqual(ExecutionStatus.Completed, empty.Run(0).Status);
        Assert.IsNull(empty.Run(0).StepIndex);
    }

    [TestMethod]
    public void StopAndFailure_AreDistinct_AndKeepTheLastPayload()
    {
        var completed = 0;
        var later = 0;
        var stopped = Pipeline<int>.Create().Then(value => value + 1).StopWhen(value => value == 2)
            .Then(value => { later++; return value; }).OnCompleted(_ => completed++).Run(1);
        Assert.AreEqual(ExecutionStatus.Stopped, stopped.Status);
        Assert.AreEqual(2, stopped.Payload);
        Assert.AreEqual(1, stopped.StepIndex);
        Assert.IsNull(stopped.Error);
        Assert.AreEqual(0, later);
        Assert.AreEqual(0, completed);

        var failure = new InvalidOperationException();
        var errors = 0;
        var failed = Pipeline<int>.Create().Then(value => value + 1).Then(value => throw failure)
            .OnError((value, error) => { errors++; Assert.AreSame(failure, error); }).Run(4);
        Assert.AreEqual(ExecutionStatus.Failed, failed.Status);
        Assert.AreEqual(5, failed.Payload);
        Assert.AreSame(failure, failed.Error);
        Assert.AreEqual(1, errors);
        Assert.AreEqual(1, failed.StepIndex);
    }

    [TestMethod]
    public void Cancellation_IsExplicit_AndDoesNotInvokeObserversOrRetries()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var calls = 0;
        var pipeline = Pipeline<int>.Create().Then(value => { calls++; return value; })
            .OnCompleted(_ => calls++).OnError((_, _) => calls++);
        var result = pipeline.Run(2, cancellation.Token);
        Assert.AreEqual(ExecutionStatus.Cancelled, result.Status);
        Assert.IsNull(result.StepIndex);
        Assert.AreEqual(0, calls);
        Assert.AreEqual(cancellation.Token, ((OperationCanceledException)result.Error!).CancellationToken);
        var exception = new OperationCanceledException();
        var fromStep = Pipeline<int>.Create().Then(value => throw exception, new RetryPolicy(3, _ => true)).Run(2);
        Assert.AreEqual(ExecutionStatus.Cancelled, fromStep.Status);
        Assert.AreSame(exception, fromStep.Error);
    }

    [TestMethod]
    public void RetryBudget_IsPerStepAndPerRun_AndObserversAreOutsideIt()
    {
        var attempts = 0;
        var errors = 0;
        var policy = new RetryPolicy(3, error => error is InvalidOperationException);
        var pipeline = Pipeline<int>.Create().Then(value =>
        {
            if (++attempts % 3 != 0) throw new InvalidOperationException();
            return value + 1;
        }, policy).OnError((_, _) => errors++);
        Assert.AreEqual(4, pipeline.Run(3).Payload);
        Assert.AreEqual(6, pipeline.Run(5).Payload);
        Assert.AreEqual(6, attempts);
        Assert.AreEqual(0, errors);

        var observerFailure = new Exception("observer");
        Assert.AreSame(observerFailure, Assert.ThrowsExactly<Exception>(() =>
            pipeline.OnCompleted(_ => throw observerFailure).Run(1)));
        var failing = Pipeline<int>.Create().Then(value => throw new InvalidOperationException(), policy)
            .OnError((_, _) => throw observerFailure);
        Assert.AreSame(observerFailure, Assert.ThrowsExactly<Exception>(() => failing.Run(1)));
    }

    [TestMethod]
    public async Task AsyncRuns_AwaitStepsAndObservers_AndKeepConcurrentPayloadsIndependent()
    {
        var firstGate = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var observerGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var observerEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var later = 0;
        var pipeline = AsyncPipeline<int>.Create().Then((value, token) => firstGate.Task)
            .Then(value => { later++; return Task.FromResult(value * 2); })
            .OnCompleted(_ => { observerEntered.SetResult(); return observerGate.Task; });
        var operation = pipeline.RunAsync(1);
        try
        {
            Assert.IsFalse(operation.IsCompleted);
            Assert.AreEqual(0, later);
            firstGate.SetResult(3);
            await observerEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.IsFalse(operation.IsCompleted, "The completion observer must also be awaited.");
        }
        finally { firstGate.TrySetResult(3); observerGate.TrySetResult(); }
        var result = await operation.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.AreEqual(ExecutionStatus.Completed, result.Status);
        Assert.AreEqual(6, result.Payload);
        Assert.AreEqual(1, later);

        var reusable = AsyncPipeline<int>.Create().Then(async value => { await Task.Yield(); return value + 1; });
        var results = await Task.WhenAll(Enumerable.Range(0, 30).Select(value => reusable.RunAsync(value)));
        CollectionAssert.AreEqual(Enumerable.Range(1, 30).ToArray(), results.Select(run => run.Payload).ToArray());
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task AsyncCancellation_AwaitsInFlightWork_ThenStopsBeforeTheNextStep(bool withRetry)
    {
        using var cancellation = new CancellationTokenSource();
        var gate = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var received = CancellationToken.None;
        var pipeline = AsyncPipeline<int>.Create().Then((value, token) => { received = token; return gate.Task; },
            withRetry ? new RetryPolicy(2, _ => true) : null)
            .Then(value => { calls++; return Task.FromResult(value); });
        var operation = pipeline.RunAsync(1, cancellation.Token);
        try
        {
            cancellation.Cancel();
            Assert.IsFalse(operation.IsCompleted);
        }
        finally { gate.TrySetResult(4); }
        var result = await operation.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.AreEqual(ExecutionStatus.Cancelled, result.Status);
        Assert.AreEqual(cancellation.Token, received);
        Assert.AreEqual(4, result.Payload);
        Assert.AreEqual(0, calls);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void CancellationAfterStepReturns_RetainsItsPayload_WithOrWithoutRetry(bool withRetry)
    {
        using var cancellation = new CancellationTokenSource();
        var pipeline = Pipeline<int>.Create().Then((value, token) => { cancellation.Cancel(); return value + 3; },
            withRetry ? new RetryPolicy(2, _ => true) : null);
        var result = pipeline.Run(1, cancellation.Token);
        Assert.AreEqual(ExecutionStatus.Cancelled, result.Status);
        Assert.AreEqual(4, result.Payload);
    }

    [TestMethod]
    public async Task AsyncFailure_RetriesOnlySteps_AndAwaitsTheErrorObserver()
    {
        var failure = new InvalidOperationException();
        var calls = 0;
        var observer = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pipeline = AsyncPipeline<int>.Create().Then(value => { calls++; return Task.FromException<int>(failure); }, new RetryPolicy(2, _ => true))
            .OnError((_, _) => { entered.SetResult(); return observer.Task; });
        var operation = pipeline.RunAsync(7);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.IsFalse(operation.IsCompleted);
        }
        finally { observer.TrySetResult(); }
        var result = await operation.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.AreEqual(ExecutionStatus.Failed, result.Status);
        Assert.AreEqual(2, calls);
        Assert.AreSame(failure, result.Error);
        var observerFailure = new Exception("observer");
        Assert.AreSame(observerFailure, await Assert.ThrowsExactlyAsync<Exception>(() =>
            pipeline.OnError((_, _) => Task.FromException(observerFailure)).RunAsync(7)));
    }

    [TestMethod]
    public async Task AsyncDefinition_Stop_DefaultCompletion_NullTask_AndPreCancellation()
    {
        var empty = AsyncPipeline<int>.Create();
        Assert.AreEqual(3, (await empty.RunAsync(3)).Payload);
        Assert.AreEqual(ExecutionStatus.Stopped, (await empty.StopWhen(value => value == 3).RunAsync(3)).Status);
        Assert.AreEqual(ExecutionStatus.Completed, (await empty.StopWhen(_ => false).RunAsync(3)).Status);
        Assert.AreEqual(ExecutionStatus.Failed, (await empty.Then(value => (Task<int>)null!).RunAsync(3)).Status);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.AreEqual(ExecutionStatus.Cancelled, (await empty.RunAsync(3, cancellation.Token)).Status);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => empty.OnCompleted(_ => null!).RunAsync(3));
        Assert.ThrowsExactly<ArgumentNullException>(() => empty.Then((Func<int, Task<int>>)null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => Pipeline<int>.Create().Then((Func<int, int>)null!));
    }
}
