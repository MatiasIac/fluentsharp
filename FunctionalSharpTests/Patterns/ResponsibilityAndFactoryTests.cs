#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FunctionalSharp.Patterns.Tests;

[TestClass]
public class ResponsibilityAndFactoryTests
{
    [TestMethod]
    public void FirstMatchingHandlerWins_AndFallbackRunsOnlyWhenUnmatched()
    {
        var calls = new List<string>();
        var empty = ChainOfResponsibility<int, string>.Create();
        var chain = empty.When(value => value > 0, value => { calls.Add("first"); return "positive"; })
            .When(value => { calls.Add("second predicate"); return true; }, value => "other");
        var result = chain.Handle(2);
        Assert.AreEqual(HandlingStatus.Handled, result.Status);
        Assert.AreEqual("positive", result.Value);
        Assert.AreEqual(0, result.HandlerIndex);
        CollectionAssert.AreEqual(new[] { "first" }, calls);
        Assert.AreEqual(HandlingStatus.Unhandled, empty.Handle(1).Status);
        Assert.ThrowsExactly<InvalidOperationException>(() => _ = empty.Handle(1).Value);
        Assert.AreEqual("fallback", empty.Otherwise(_ => "fallback").Handle(1).Value);
        Assert.AreEqual(HandlingStatus.Unhandled, empty.Handle(1).Status);
    }

    [TestMethod]
    public void HandlerAndPredicateFailures_DoNotFallThrough_AndCancellationIsDistinct()
    {
        var failure = new InvalidOperationException();
        var fallbackCalls = 0;
        foreach (var predicateFails in new[] { true, false })
        {
            var chain = ChainOfResponsibility<int, string>.Create()
                .When(value => predicateFails ? throw failure : true, value => throw failure)
                .Otherwise(value => { fallbackCalls++; return "fallback"; });
            var result = chain.Handle(1);
            Assert.AreEqual(HandlingStatus.Failed, result.Status);
            Assert.AreSame(failure, result.Error);
        }
        Assert.AreEqual(0, fallbackCalls);
        var cancelled = ChainOfResponsibility<int, string>.Create().When(_ => true, _ => throw new OperationCanceledException()).Handle(1);
        Assert.AreEqual(HandlingStatus.Cancelled, cancelled.Status);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.AreEqual(HandlingStatus.Cancelled, ChainOfResponsibility<int, string>.Create().Handle(1, cancellation.Token).Status);
    }

    [TestMethod]
    public void CancellationInsidePredicate_PreventsHandler_AndNullCanBeAHandledResponse()
    {
        using var cancellation = new CancellationTokenSource();
        var calls = 0;
        var chain = ChainOfResponsibility<int, string>.Create().When(_ => { cancellation.Cancel(); return true; }, _ => { calls++; return "handled"; });
        Assert.AreEqual(HandlingStatus.Cancelled, chain.Handle(1, cancellation.Token).Status);
        Assert.AreEqual(0, calls);
        var result = ChainOfResponsibility<int, string?>.Create().Otherwise(_ => null).Handle(1);
        Assert.AreEqual(HandlingStatus.Handled, result.Status);
        Assert.IsNull(result.Value);
    }

    [TestMethod]
    public async Task AsyncPredicatesAndHandlers_AreAwaited_AndOnlyFirstMatchRuns()
    {
        var predicate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var laterCalls = 0;
        var chain = AsyncChainOfResponsibility<int, string>.Create()
            .When((value, token) => predicate.Task, (value, token) => { entered.SetResult(); return handler.Task; })
            .When(_ => { laterCalls++; return true; }, _ => Task.FromResult("later"));
        var operation = chain.HandleAsync(1);
        try
        {
            Assert.IsFalse(operation.IsCompleted);
            Assert.IsFalse(entered.Task.IsCompleted);
            predicate.SetResult(true);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.IsFalse(operation.IsCompleted);
        }
        finally { predicate.TrySetResult(true); handler.TrySetResult("first"); }
        var result = await operation.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.AreEqual("first", result.Value);
        Assert.AreEqual(0, laterCalls);
    }

    [TestMethod]
    public async Task AsyncCancellation_WaitsForHandler_AndDoesNotUseFallback()
    {
        using var cancellation = new CancellationTokenSource();
        var gate = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var received = CancellationToken.None;
        var chain = AsyncChainOfResponsibility<int, string>.Create()
            .When(_ => true, (_, token) => { received = token; return gate.Task; })
            .Otherwise(_ => throw new Exception("Must not run"));
        var operation = chain.HandleAsync(1, cancellation.Token);
        try { cancellation.Cancel(); Assert.IsFalse(operation.IsCompleted); }
        finally { gate.TrySetResult("value"); }
        var result = await operation.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.AreEqual(HandlingStatus.Cancelled, result.Status);
        Assert.AreEqual(cancellation.Token, received);
    }

    [TestMethod]
    public async Task AsyncChains_HaveIndependentDefinitions_AndExplicitFailureOutcomes()
    {
        var empty = AsyncChainOfResponsibility<int, string>.Create();
        Assert.AreEqual(HandlingStatus.Unhandled, (await empty.HandleAsync(1)).Status);
        Assert.AreEqual("fallback", (await empty.Otherwise(_ => Task.FromResult("fallback")).HandleAsync(1)).Value);
        Assert.AreEqual(HandlingStatus.Unhandled, (await empty.HandleAsync(1)).Status);
        var failure = new Exception("handler");
        var result = await empty.When(_ => true, _ => Task.FromException<string>(failure)).HandleAsync(1);
        Assert.AreEqual(HandlingStatus.Failed, result.Status);
        Assert.AreSame(failure, result.Error);
        Assert.AreEqual(HandlingStatus.Failed, (await empty.When((_, _) => null!, (_, _) => Task.FromResult("unused")).HandleAsync(1)).Status);
        Assert.AreEqual(HandlingStatus.Failed, (await empty.When(_ => true, _ => (Task<string>)null!).HandleAsync(1)).Status);
        Assert.AreEqual(HandlingStatus.Failed, (await empty.Otherwise(_ => (Task<string>)null!).HandleAsync(1)).Status);
    }

    [TestMethod]
    public void Factory_BuildIsASnapshot_AndOnlySelectedConstructorsRun()
    {
        var calls = 0;
        var builder = Factory.For<string, object>(StringComparer.OrdinalIgnoreCase)
            .Register("email", () => { calls++; return new object(); });
        var first = builder.Build();
        builder.Register("sms", () => new object());
        var second = builder.Build();
        Assert.AreEqual(0, calls);
        Assert.AreNotSame(first.Create("EMAIL"), first.Create("email"));
        Assert.AreEqual(2, calls);
        Assert.IsFalse(first.TryCreate("sms", out var absent));
        Assert.IsNull(absent);
        Assert.IsTrue(second.TryCreate("sms", out var present));
        Assert.IsNotNull(present);
        Assert.ThrowsExactly<ArgumentException>(() => builder.Register("EMAIL", () => new object()));
        Assert.ThrowsExactly<KeyNotFoundException>(() => first.Create("unknown"));
    }

    [TestMethod]
    public void Factory_RejectsNullRegistrations_AndPropagatesConstructorFailures()
    {
        var builder = Factory.For<string, object>();
        Assert.ThrowsExactly<ArgumentNullException>(() => builder.Register(null!, () => new object()));
        Assert.ThrowsExactly<ArgumentNullException>(() => builder.Register("null", null!));
        var failure = new InvalidOperationException();
        var factory = builder.Register("bad", () => throw failure).Build();
        Assert.AreSame(failure, Assert.ThrowsExactly<InvalidOperationException>(() => factory.Create("bad")));
        Assert.AreSame(failure, Assert.ThrowsExactly<InvalidOperationException>(() => factory.TryCreate("bad", out _)));
        Assert.ThrowsExactly<ArgumentNullException>(() => factory.Create(null!));
    }
}
