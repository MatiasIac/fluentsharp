#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FunctionalSharp.Patterns.Tests;

[TestClass]
public class RetryPolicyTests
{
    [TestMethod]
    [DataRow(1)]
    [DataRow(3)]
    public void Exhaustion_UsesExactlyTheAttemptBudget_AndRethrowsOriginalFailure(int maxAttempts)
    {
        var failure = new InvalidOperationException();
        var calls = 0;
        var predicates = 0;
        var policy = new RetryPolicy(maxAttempts, _ => { predicates++; return true; });
        var error = Assert.ThrowsExactly<InvalidOperationException>(() => policy.Execute(() => { calls++; throw failure; }));
        Assert.AreSame(failure, error);
        Assert.AreEqual(maxAttempts, calls);
        Assert.AreEqual(maxAttempts - 1, predicates);
    }

    [TestMethod]
    public void NonRetryableAndPredicateFailures_EscapeImmediately()
    {
        var failure = new InvalidOperationException();
        var calls = 0;
        Assert.AreSame(failure, Assert.ThrowsExactly<InvalidOperationException>(() => new RetryPolicy(3, _ => false)
            .Execute(() => { calls++; throw failure; })));
        Assert.AreEqual(1, calls);
        var predicateFailure = new Exception("predicate");
        Assert.AreSame(predicateFailure, Assert.ThrowsExactly<Exception>(() => new RetryPolicy(3, _ => throw predicateFailure).Execute(() => throw failure)));
    }

    [TestMethod]
    public void Cancellation_NeverInvokesRetryPredicate_AndPreCancellationSkipsWork()
    {
        var policy = new RetryPolicy(3, _ => throw new Exception("Must not retry cancellation"));
        var failure = new OperationCanceledException();
        Assert.AreSame(failure, Assert.ThrowsExactly<OperationCanceledException>(() => policy.Execute(() => throw failure)));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.ThrowsExactly<OperationCanceledException>(() => policy.Execute(() => throw new Exception("Must not run"), cancellation.Token));
    }

    [TestMethod]
    public async Task AsyncRetry_AwaitsFailedAttempts_AndForwardsTheToken()
    {
        using var cancellation = new CancellationTokenSource();
        var first = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var policy = new RetryPolicy(2, _ => true);
        var operation = policy.ExecuteAsync(token =>
        {
            Assert.AreEqual(cancellation.Token, token);
            return ++calls == 1 ? first.Task : Task.FromResult(7);
        }, cancellation.Token);
        try { Assert.IsFalse(operation.IsCompleted); Assert.AreEqual(1, calls); }
        finally { first.TrySetException(new InvalidOperationException()); }
        Assert.AreEqual(7, await operation.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.AreEqual(2, calls);
    }

    [TestMethod]
    public async Task AsyncCancellation_WaitsForRunningWork_ThenDoesNotRetry()
    {
        using var cancellation = new CancellationTokenSource();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var policy = new RetryPolicy(3, _ => throw new Exception("Must not retry"));
        var operation = policy.ExecuteAsync(token => { calls++; return gate.Task; }, cancellation.Token);
        try { cancellation.Cancel(); Assert.IsFalse(operation.IsCompleted); }
        finally { gate.TrySetResult(); }
        var error = await Assert.ThrowsAsync<OperationCanceledException>(() => operation.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.AreEqual(cancellation.Token, error.CancellationToken);
        Assert.AreEqual(1, calls);
    }

    [TestMethod]
    public async Task Retry_ValidatesConfiguration_Delegates_AndNullTasks()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new RetryPolicy(0, _ => true));
        Assert.ThrowsExactly<ArgumentNullException>(() => new RetryPolicy(2, null!));
        var policy = new RetryPolicy(1, _ => true);
        Assert.ThrowsExactly<ArgumentNullException>(() => policy.Execute((Action)null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => policy.Execute<int>(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => policy.ExecuteAsync((Func<CancellationToken, Task>)null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => policy.ExecuteAsync<int>(null!));
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => policy.ExecuteAsync(token => (Task)null!));
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => policy.ExecuteAsync<int>(token => null!));
    }
}
