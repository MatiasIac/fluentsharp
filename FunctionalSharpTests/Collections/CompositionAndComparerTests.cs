#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using FunctionalSharp.Collections;
using FunctionalSharp.Composition;
using FunctionalSharp.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FunctionalSharp.Tests;

[TestClass]
public class CompositionAndComparerTests
{
    [TestMethod]
    public void TapAndPipe_DoNotEnumerate_AndIterationDisposesOnStoppingAndFailure()
    {
        var enumerations = 0;
        var disposed = 0;
        IEnumerable<int> Sequence()
        {
            enumerations++;
            if (enumerations > 1) throw new Exception("Second enumeration");
            try { yield return 1; yield return 2; yield return 3; }
            finally { disposed++; }
        }
        var sequence = Sequence();
        Assert.AreSame(sequence, sequence.Tap(_ => { }).Pipe(items => items));
        Assert.AreEqual(0, enumerations);
        sequence.For(item => item < 2, _ => { });
        Assert.AreEqual(1, disposed);
        enumerations = 0;
        Assert.ThrowsExactly<InvalidOperationException>(() => Sequence().ForEvery(_ => throw new InvalidOperationException()));
        Assert.AreEqual(2, disposed);
    }

    [TestMethod]
    public void SynchronousIteration_RejectsNullArgumentsEvenForEmptyInputs()
    {
        int[] empty = [];
        Assert.ThrowsExactly<ArgumentNullException>(() => empty.ForEvery(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => empty.For((Func<int, bool>)null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => empty.For((Func<int, bool>)null!, _ => { }));
        Assert.ThrowsExactly<ArgumentNullException>(() => empty.For((Func<int, int, bool>)null!, _ => { }));
        Assert.ThrowsExactly<ArgumentNullException>(() => empty.For(_ => true, null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => empty.For((_, index) => true, null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => ((int[])null!).ForEvery(_ => { }));
    }

    [TestMethod]
    public void KeyComparer_WorksAcrossHashSets_Dictionaries_AndJoins()
    {
        var comparer = KeyComparer.By((Person person) => person.Name, StringComparer.OrdinalIgnoreCase);
        var first = new Person("Alice");
        var same = new Person("ALICE");
        var other = new Person("Bob");
        Assert.IsTrue(comparer.Equals(first, same));
        Assert.AreEqual(comparer.GetHashCode(first), comparer.GetHashCode(same));
        var set = new HashSet<Person>(comparer) { first, same, other };
        Assert.AreEqual(2, set.Count);
        var dictionary = new Dictionary<Person, int>(comparer) { [first] = 7 };
        Assert.AreEqual(7, dictionary[same]);
        Assert.AreEqual(1, new[] { first, other }.Join(new[] { same }, p => p, p => p, (a, b) => a, comparer).Count());
    }

    [TestMethod]
    public void KeyComparer_NullsDoNotInvokeSelector_AndNullKeysHashConsistently()
    {
        var calls = 0;
        var comparer = KeyComparer.By((Person person) => { calls++; return person.Name; });
        Assert.IsTrue(comparer.Equals(null, null));
        Assert.IsFalse(comparer.Equals(null, new Person(null)));
        Assert.IsFalse(comparer.Equals(new Person(null), null));
        Assert.AreEqual(0, comparer.GetHashCode(null!));
        Assert.AreEqual(0, calls);
        Assert.IsTrue(comparer.Equals(new Person(null), new Person(null)));
        Assert.AreEqual(0, comparer.GetHashCode(new Person(null)));
        Assert.ThrowsExactly<ArgumentNullException>(() => KeyComparer.By<Person, string>(null!));
    }

    [TestMethod]
    public void MatchingHashDelegates_AreAvailableAcrossAllLinqShapes()
    {
        var values = new[] { "a", "A", "b" };
        var second = new[] { "A" };
        Func<string?, string?, bool> equal = StringComparer.OrdinalIgnoreCase.Equals;
        Func<string, int> hash = StringComparer.OrdinalIgnoreCase.GetHashCode;
        Assert.AreEqual(2, values.Distinct(equal, hash).Count());
        Assert.AreEqual(1, values.Intersect(second, equal, hash).Count());
        Assert.AreEqual(1, values.Except(second, equal, hash).Count());
        Assert.AreEqual(2, values.Union(second, equal, hash).Count());
        Assert.IsTrue(new[] { "a" }.Contains("A", equal, hash));
        Assert.AreEqual(2, values.GroupBy(x => x, equal, hash).Count());
        Assert.AreEqual(2, values.GroupBy(x => x, x => x.Length, equal, hash).Count());
        Assert.AreEqual(2, values.GroupBy(x => x, (key, items) => items.Count(), equal, hash).Count());
        Assert.AreEqual(2, values.GroupBy(x => x, x => x.Length, (key, items) => items.Sum(), equal, hash).Count());
        Assert.AreEqual(2, values.Join(second, x => x, x => x, (a, b) => a, equal, hash).Count());
        Assert.AreEqual(2, values.GroupJoin(second, x => x, x => x, (a, items) => items.Count(), equal, hash).Sum());
        Assert.AreEqual(2, values.ToLookup(x => x, equal, hash).Count);
        Assert.AreEqual(2, values.ToLookup(x => x, x => x.Length, equal, hash).Count);
        Assert.AreEqual(1, new[] { "a" }.ToDictionary(x => x, x => x.Length, equal, hash)["A"]);
    }

    [TestMethod]
    public void ExplicitHashQueries_RemainDeferred_AndPropagateHashFailures()
    {
        var calls = 0;
        var failure = new InvalidOperationException();
        var query = new[] { "a" }.Distinct(StringComparer.OrdinalIgnoreCase.Equals,
            value => { calls++; throw failure; });
        Assert.AreEqual(0, calls);
        Assert.AreSame(failure, Assert.ThrowsExactly<InvalidOperationException>(() => query.ToList()));
        Assert.AreEqual(1, calls);
    }

    private sealed record Person(string? Name);
}
