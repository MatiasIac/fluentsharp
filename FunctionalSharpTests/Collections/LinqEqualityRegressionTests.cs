using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace FunctionalSharp.Linq.Tests
{
    [TestClass]
    public class LinqEqualityRegressionTests
    {
        // Equal keys deliberately have different built-in hash codes: 1, 11, and 21.
        private static bool SameLastDigit(int left, int right) => left % 10 == right % 10;

        [TestMethod]
        public void Distinct_UsesDelegateEquality_WhenDefaultHashCodesDiffer()
        {
            var result = new[] { 1, 11, 2, 12, 3 }.Distinct(SameLastDigit).ToArray();

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, result);
        }

        [TestMethod]
        public void Distinct_PreservesFirstObject_WhenComparedPropertiesMatch()
        {
            var first = new KeyWithHash(1, 101);
            var duplicate = new KeyWithHash(1, 202);
            var other = new KeyWithHash(2, 303);

            var result = new[] { first, duplicate, other }
                .Distinct((left, right) => left.Key == right.Key).ToArray();

            CollectionAssert.AreEqual(new[] { first, other }, result);
        }

        [TestMethod]
        public void Intersect_UsesDelegateEquality_AndReturnsFirstDistinctMatches()
        {
            var result = new[] { 1, 11, 2, 3 }
                .Intersect(new[] { 21, 12 }, SameLastDigit).ToArray();

            CollectionAssert.AreEqual(new[] { 1, 2 }, result);
        }

        [TestMethod]
        public void Except_UsesDelegateEquality_ForExclusionAndDeduplication()
        {
            var result = new[] { 1, 11, 2, 12, 3 }
                .Except(new[] { 21 }, SameLastDigit).ToArray();

            CollectionAssert.AreEqual(new[] { 2, 3 }, result);
        }

        [TestMethod]
        public void Union_UsesDelegateEquality_AcrossBothSequences()
        {
            var result = new[] { 1, 11, 2 }
                .Union(new[] { 21, 12, 3 }, SameLastDigit).ToArray();

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, result);
        }

        [TestMethod]
        public void Contains_UsesDelegateEquality_ForMatchingAndMissingValues()
        {
            var source = new[] { 1, 2, 3 };

            Assert.IsTrue(source.Contains(11, SameLastDigit));
            Assert.IsFalse(source.Contains(14, SameLastDigit));
        }

        [TestMethod]
        public void GroupBy_MergesEqualKeys_AndPreservesGroupOrder()
        {
            var groups = new[] { 1, 11, 2, 12, 3 }
                .GroupBy(value => value, SameLastDigit).ToArray();

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, groups.Select(group => group.Key).ToArray());
            CollectionAssert.AreEqual(new[] { 1, 11 }, groups[0].ToArray());
            CollectionAssert.AreEqual(new[] { 2, 12 }, groups[1].ToArray());
            CollectionAssert.AreEqual(new[] { 3 }, groups[2].ToArray());
        }

        [TestMethod]
        public void GroupBy_WithElementSelector_MergesEqualKeys()
        {
            var groups = new[] { 1, 11, 2 }
                .GroupBy(value => value, value => value.ToString(), SameLastDigit).ToArray();

            Assert.AreEqual(2, groups.Length);
            CollectionAssert.AreEqual(new[] { "1", "11" }, groups[0].ToArray());
            CollectionAssert.AreEqual(new[] { "2" }, groups[1].ToArray());
        }

        [TestMethod]
        public void GroupBy_WithResultSelector_MergesEqualKeys()
        {
            var result = new[] { 1, 11, 2 }.GroupBy(
                value => value,
                (key, values) => $"{key}:{string.Join(",", values)}",
                SameLastDigit).ToArray();

            CollectionAssert.AreEqual(new[] { "1:1,11", "2:2" }, result);
        }

        [TestMethod]
        public void GroupBy_WithElementAndResultSelectors_MergesEqualKeys()
        {
            var result = new[] { 1, 11, 2 }.GroupBy(
                value => value,
                value => value * 2,
                (key, values) => $"{key}:{string.Join(",", values)}",
                SameLastDigit).ToArray();

            CollectionAssert.AreEqual(new[] { "1:2,22", "2:4" }, result);
        }

        [TestMethod]
        public void Join_MatchesEqualKeys_WithDifferentHashCodes()
        {
            var result = new[] { 1, 2, 3 }.Join(
                new[] { 11, 21, 12 },
                outer => outer,
                inner => inner,
                (outer, inner) => $"{outer}:{inner}",
                SameLastDigit).ToArray();

            CollectionAssert.AreEqual(new[] { "1:11", "1:21", "2:12" }, result);
        }

        [TestMethod]
        public void GroupJoin_MatchesEqualKeys_AndKeepsUnmatchedOuterValues()
        {
            var result = new[] { 1, 2, 3 }.GroupJoin(
                new[] { 11, 21, 12 },
                outer => outer,
                inner => inner,
                (outer, matches) => $"{outer}:{string.Join(",", matches)}",
                SameLastDigit).ToArray();

            CollectionAssert.AreEqual(new[] { "1:11,21", "2:12", "3:" }, result);
        }

        [TestMethod]
        public void ToDictionary_FindsValuesUsingEquivalentKeys()
        {
            var dictionary = new[] { 1, 2 }.ToDictionary(
                value => value, value => value.ToString(), SameLastDigit);

            Assert.AreEqual("1", dictionary[21]);
            Assert.AreEqual("2", dictionary[12]);
            Assert.IsFalse(dictionary.ContainsKey(3));
        }

        [TestMethod]
        public void ToDictionary_RejectsEquivalentDuplicateKeys()
        {
            Assert.ThrowsExactly<ArgumentException>(() => new[] { 1, 11 }.ToDictionary(
                value => value, value => value.ToString(), SameLastDigit));
        }

        [TestMethod]
        public void ToLookup_MergesEqualKeys_AndFindsEquivalentKeys()
        {
            var lookup = new[] { 1, 11, 2 }.ToLookup(value => value, SameLastDigit);

            Assert.AreEqual(2, lookup.Count);
            CollectionAssert.AreEqual(new[] { 1, 11 }, lookup[21].ToArray());
            CollectionAssert.AreEqual(new[] { 2 }, lookup[12].ToArray());
            Assert.AreEqual(0, lookup[3].Count());
        }

        [TestMethod]
        public void ToLookup_WithElementSelector_MergesEqualKeys()
        {
            var lookup = new[] { 1, 11, 2 }.ToLookup(
                value => value, value => value.ToString(), SameLastDigit);

            Assert.AreEqual(2, lookup.Count);
            CollectionAssert.AreEqual(new[] { "1", "11" }, lookup[21].ToArray());
            CollectionAssert.AreEqual(new[] { "2" }, lookup[12].ToArray());
        }

        [TestMethod]
        public void Distinct_HandlesNulls_WithANullAwareEqualityDelegate()
        {
            var result = new[] { null, "apple", "APPLE", null, "pear" }
                .Distinct(StringComparer.OrdinalIgnoreCase.Equals).ToArray();

            CollectionAssert.AreEqual(new[] { null, "apple", "pear" }, result);
        }

        [TestMethod]
        public void GroupingAndLookup_PreserveNullKeyGroups()
        {
            var source = new[] { null, "apple", null, "APPLE" };
            var groups = source.GroupBy(value => value, StringComparer.OrdinalIgnoreCase.Equals).ToArray();
            var lookup = source.ToLookup(value => value, StringComparer.OrdinalIgnoreCase.Equals);

            Assert.AreEqual(2, groups.Length);
            Assert.IsNull(groups[0].Key);
            Assert.AreEqual(2, groups[0].Count());
            Assert.AreEqual(2, lookup.Count);
            Assert.AreEqual(2, lookup[null].Count());
            CollectionAssert.AreEqual(new[] { "apple", "APPLE" }, lookup["Apple"].ToArray());
        }

        [TestMethod]
        public void Join_KeepsStandardLinqNullKeyBehavior()
        {
            var result = new[] { null, "apple" }.Join(
                new[] { null, "APPLE" },
                outer => outer,
                inner => inner,
                (outer, inner) => $"{outer}:{inner}",
                StringComparer.OrdinalIgnoreCase.Equals).ToArray();

            CollectionAssert.AreEqual(new[] { "apple:APPLE" }, result);
        }

        [TestMethod]
        public void Distinct_WithExplicitHash_ResolvesCollisionsUsingEquality()
        {
            var result = new[] { 1, 2, 1, 3 }.Distinct((left, right) => left == right, value => 0).ToArray();

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, result);
        }

        [TestMethod]
        public void Distinct_UsesSuppliedHash_WithoutRequiringAParameterlessConstructor()
        {
            var hashCalls = 0;
            var result = new[] { "apple", "APPLE", "pear" }.Distinct(
                StringComparer.OrdinalIgnoreCase.Equals,
                value =>
                {
                    hashCalls++;
                    return StringComparer.OrdinalIgnoreCase.GetHashCode(value);
                }).ToArray();

            CollectionAssert.AreEqual(new[] { "apple", "pear" }, result);
            Assert.IsTrue(hashCalls > 0);
        }

        [TestMethod]
        public void Distinct_WithNullHashDelegate_UsesSafeFallback()
        {
            var result = new[] { 1, 11, 2 }.Distinct(SameLastDigit, null).ToArray();

            CollectionAssert.AreEqual(new[] { 1, 2 }, result);
        }

        [TestMethod]
        public void Distinct_RejectsNullEqualityDelegate_BeforeEnumeration()
        {
            var error = Assert.ThrowsExactly<ArgumentNullException>(() =>
                new int[0].Distinct((Func<int, int, bool>)null));

            Assert.AreEqual("comparer", error.ParamName);
        }

        [TestMethod]
        public void Distinct_RemainsDeferred_AndCanBeEnumeratedAgain()
        {
            var comparisons = 0;
            var query = new[] { 1, 11, 2 }.Distinct((left, right) =>
            {
                comparisons++;
                return SameLastDigit(left, right);
            });

            Assert.AreEqual(0, comparisons);
            CollectionAssert.AreEqual(new[] { 1, 2 }, query.ToArray());
            Assert.IsTrue(comparisons > 0);
            CollectionAssert.AreEqual(new[] { 1, 2 }, query.ToArray());
        }

        private sealed class KeyWithHash
        {
            private readonly int hashCode;

            public KeyWithHash(int key, int hashCode)
            {
                Key = key;
                this.hashCode = hashCode;
            }

            public int Key { get; }

            public override int GetHashCode() => hashCode;
        }
    }
}
