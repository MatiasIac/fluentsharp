using Microsoft.VisualStudio.TestTools.UnitTesting;
using FunctionalSharp.Collections;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace FunctionalSharp.Collections.Tests
{
    [TestClass()]
    public class IteratorsTests
    {
        private IEnumerable<int> intCollection;

        [TestInitialize()]
        public void Setup()
        {
            intCollection = new List<int>() { 20, 21, 55, 77, 1 };
        }

        [TestMethod()]
        public void When_Then_ProcessIntCollection_Expect_SumValues()
        {
            var sum = 0;
            var set = intCollection
                .Where(i => i <= 23)
                .Then(collection => sum = collection.Sum(i => i));

            Assert.AreEqual(42, sum);
        }

        [TestMethod()]
        public void When_ForEvery_SumIntCollection_Expect_Values()
        {
            var sum = 0;
            intCollection
                .ForEvery(i => sum += i);

            Assert.AreEqual(intCollection.Sum(i => i), sum);
        }

        [TestMethod()]
        public void When_For_CannotContinue_Expect_Sum()
        {
            var sum = 0;
            
            intCollection.For(
                item => item < 22, 
                item => sum += item
            );

            Assert.AreEqual(41, sum);
        }

        [TestMethod()]
        public void When_For_CannotContinue_Expect_LastIndex()
        {
            var index = -1;
            var sum = 0;

            intCollection.For(
                (item, i) => { index = i; return item < 22; },
                item => sum += item
            );

            Assert.AreEqual(2, index);
            Assert.AreEqual(41, sum);
        }
    }
}