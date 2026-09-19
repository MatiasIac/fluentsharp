#nullable enable
using System;
using System.Collections.Generic;
using FunctionalSharp.Composition;
using FunctionalSharp.Operations;
using FunctionalSharp.Validators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FunctionalSharp.Tests;

[TestClass]
public class FluentCoreTests
{
    [TestMethod]
    public void Condition_CapturesOnce_AndChainsWithoutMutation()
    {
        var value = true;
        var condition = value.IfTrue;
        value = false;
        var calls = 0;
        condition.Then(() => calls++).Then(() => calls++);
        Assert.AreEqual(2, calls);
        Assert.IsTrue(condition.IsMatched);
        Assert.IsFalse(value.IfTrue.IsMatched);
        Assert.IsTrue(value.IfFalse.IsMatched);
    }

    [TestMethod]
    public void InactiveCondition_DoesNotConstructExceptionOrEvaluateArguments()
    {
        var messages = 0;
        DetailedException.Calls = 0;
        string BuildMessage() { messages++; return "invalid"; }
        false.IfTrue.Throw<DetailedException>(() => new(BuildMessage(), 7));
        default(Condition).Throw<DetailedException>(() => new(BuildMessage(), 7));
        Assert.AreEqual(0, messages);
        Assert.AreEqual(0, DetailedException.Calls);

        var error = Assert.ThrowsExactly<DetailedException>(() => true.IfTrue.Throw(() => new DetailedException(BuildMessage(), 7)));
        Assert.AreEqual(1, messages);
        Assert.AreEqual(1, DetailedException.Calls);
        Assert.AreEqual(7, error.Code);
    }

    [TestMethod]
    public void ParameterlessException_IsLazy_AndFactoryFailuresPropagate()
    {
        CountedException.Calls = 0;
        false.IfTrue.Throw<CountedException>();
        Assert.AreEqual(0, CountedException.Calls);
        Assert.ThrowsExactly<CountedException>(() => true.IfTrue.Throw<CountedException>());
        Assert.AreEqual(1, CountedException.Calls);
        var failure = new InvalidOperationException();
        Assert.AreSame(failure, Assert.ThrowsExactly<InvalidOperationException>(() => true.IfTrue.Throw<Exception>(() => throw failure)));
        Assert.ThrowsExactly<InvalidOperationException>(() => true.IfTrue.Throw<Exception>(() => null!));
    }

    [TestMethod]
    public void NullDelegates_AreInvalidEvenOnInactiveConditions()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => false.IfTrue.Then(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => false.IfTrue.Throw<Exception>(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => true.IfTrue.Match(() => 1, null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => false.IfTrue.Match<int>(null!, () => 1));
        Assert.ThrowsExactly<ArgumentNullException>(() => default(ValueCondition<string>).Then(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => default(ValueCondition<string>).Throw<Exception>(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => default(ValueCondition<string>).Match<int>(null!, () => 1));
        Assert.ThrowsExactly<ArgumentNullException>(() => "value".IfNotNull.Match(s => s.Length, null!));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Match_EvaluatesExactlyOneBranch(bool condition)
    {
        var first = 0;
        var second = 0;
        var result = condition.IfTrue.Match(() => { first++; return "yes"; }, () => { second++; return "no"; });
        Assert.AreEqual(condition ? "yes" : "no", result);
        Assert.AreEqual(condition ? 1 : 0, first);
        Assert.AreEqual(condition ? 0 : 1, second);
    }

    [TestMethod]
    public void ReferenceChecks_PreserveTheCapturedType_AndLazyFallback()
    {
        string? value = "Alice";
        var captured = value.IfNotNull;
        value = null;
        var length = 0;
        captured.Then(text => length = text.Length);
        Assert.AreEqual(5, length);
        Assert.AreEqual("Alice", captured.Match(text => text, () => throw new Exception()));
        Assert.IsTrue(value.IfNull.IsMatched);
        Assert.AreEqual("missing", value.IfNotNull.Match(text => throw new Exception(), () => "missing"));
        value.IfNotNull.Throw<Exception>(text => throw new Exception("Must not execute"));
        var error = Assert.ThrowsExactly<ArgumentException>(() => captured.Throw(text => new ArgumentException(text)));
        Assert.AreEqual("Alice", error.Message);
        Assert.IsFalse(default(ValueCondition<string>).IsMatched);
    }

    [TestMethod]
    public void NullableValues_ExposeTheUnderlyingType_AndDistinguishZeroFromNull()
    {
        int? zero = 0;
        int? absent = null;
        int received = -1;
        zero.IfNotNull.Then(number => received = number);
        Assert.AreEqual(0, received);
        Assert.IsFalse(zero.IfNull.IsMatched);
        Assert.IsTrue(absent.IfNull.IsMatched);
        Assert.AreEqual(8, absent.IfNotNull.Match(number => number, () => 8));
        Assert.IsFalse(default(ValueCondition<int>).IsMatched);
    }

    [TestMethod]
    public void PipeAndTap_KeepTypes_Order_Identity_AndNullValues()
    {
        var source = new List<int>();
        List<int> original = source.Tap(items => items.Add(3));
        var result = original.Pipe(items => items[0]).Pipe(number => number.ToString());
        Assert.AreSame(source, original);
        Assert.AreEqual("3", result);
        string? absent = null;
        Assert.IsNull(absent.Tap(text => Assert.IsNull(text)));
        Assert.AreEqual("fallback", absent.Pipe(text => text ?? "fallback"));
        Assert.ThrowsExactly<ArgumentNullException>(() => source.Tap(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => source.Pipe<List<int>, int>(null!));
    }

    [TestMethod]
    public void PipeAndTap_PropagateOriginalFailures()
    {
        var failure = new InvalidOperationException();
        Assert.AreSame(failure, Assert.ThrowsExactly<InvalidOperationException>(() => 1.Tap(_ => throw failure)));
        Assert.AreSame(failure, Assert.ThrowsExactly<InvalidOperationException>(() => 1.Pipe<int, int>(_ => throw failure)));
    }

    public sealed class CountedException : Exception
    {
        public static int Calls;
        public CountedException() => Calls++;
    }

    private sealed class DetailedException : Exception
    {
        public static int Calls;
        public int Code { get; }
        public DetailedException(string message, int code) : base(message) { Calls++; Code = code; }
    }
}
