using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FunctionalSharp.Linq.Tests
{
    [TestClass()]
    public class LinqExtendersTests
    {

        private IEnumerable<int> A;
        private IEnumerable<int> B;
        private IEnumerable<int> D;
        private IEnumerable<int> E;

        [TestInitialize]
        public void Setup()
        {
            A = new List<int> { 1, 2, 3, 4, 5, 6, 10, 11, 12, 13 };
            B = new List<int> { 6, 7, 8, 9, 10, 14, 15, 16, 17, 18 };
            D = new List<int> { 10, 11, 12, 13, 14, 15, 16, 17, 18 };
            E = new List<int> { 1, 2, 3, 4, 5, 6, 10, 11, 12, 13, 14, 15, 16, 17, 18 };
        }

        [TestMethod()]
        public void When_Intersect_Intersects_Get_Results()
        {
            var C = A.Intersect(B, (a, b) => a == b);

            Assert.AreEqual(2, C.Count());
            Assert.IsTrue(C.Contains(6));
            Assert.IsTrue(C.Contains(10));
        }

        [TestMethod()]
        public void When_Except_RemovesFromLeft_Get_Results()
        {
            var C = A.Except(D, (a, d) => a == d);
            var expectedList = new List<int> { 1, 2, 3, 4, 5, 6 };

            Assert.AreEqual(6, C.Count());
            Assert.IsTrue(C.All(c => expectedList.Contains(c)));
        }

        [TestMethod()]
        public void When_Except_RemovesFromRight_Get_Results()
        {
            var C = A.Except(E, (a, e) => a != e);
            var expectedList = new List<int> { 1, 2, 3, 4, 5, 6, 10, 11, 12, 13 };

            Assert.AreEqual(10, C.Count());
            Assert.IsTrue(C.All(c => expectedList.Contains(c)));
        }
    }
}