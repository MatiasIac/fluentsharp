using FunctionalSharp.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FunctionalSharp.Linq.Tests
{
    [TestClass()]
    public class LinqExtendersTests
    {

        private class User
        {
            public int Id { get; set; }
            public string Name { get; set; }

            public override int GetHashCode() => Id.GetHashCode();
        }

        private IEnumerable<int> A;
        private IEnumerable<int> B;
        private IEnumerable<int> D;
        private IEnumerable<int> E;
        private IEnumerable<int> similarIntList;
        private IEnumerable<User> users;
        private IEnumerable<User> repeatedUsersList;

        [TestInitialize]
        public void Setup()
        {
            A = new List<int> { 1, 2, 3, 4, 5, 6, 10, 11, 12, 13 };
            B = new List<int> { 6, 7, 8, 9, 10, 14, 15, 16, 17, 18 };
            D = new List<int> { 10, 11, 12, 13, 14, 15, 16, 17, 18 };
            E = new List<int> { 1, 2, 3, 4, 5, 6, 10, 11, 12, 13, 14, 15, 16, 17, 18 };
            similarIntList = new List<int> { 1, 1, 1, 2, 2, 2, 3, 3, 4, 4, 5 };

            users = new List<User> { 
                new User { Id = 1, Name = "John"},
                new User { Id = 2, Name = "Julia"},
                new User { Id = 3, Name = "Ron"},
            };

            repeatedUsersList = new List<User> {
                new User { Id = 1, Name = "John"},
                new User { Id = 1, Name = "John"},
                new User { Id = 1, Name = "John"},
                new User { Id = 2, Name = "Julia"},
                new User { Id = 2, Name = "Julia"},
                new User { Id = 2, Name = "Julia"},
                new User { Id = 3, Name = "Ron"},
                new User { Id = 3, Name = "Ron"},
                new User { Id = 3, Name = "Ron"},
            };
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

        [TestMethod()]
        public void When_Contains_MakeAComparison_Return_True()
        {
            var result = A.Contains(10, (a, b) => a == b);

            Assert.IsTrue(result);
        }

        [TestMethod()]
        public void When_Contains_ComparesObjectsBySingleProperty_Return_True()
        {
            var result = users.Contains(new User { Name = "Ron" }, (a, b) => a.Name == b.Name);
            Assert.IsTrue(result);
        }

        [TestMethod()]
        public void When_Distinct_Process_Return_UniqueValues()
        {
            var result = similarIntList.Distinct((a, b) => a == b);
            Assert.AreEqual(5, result.Count());
        }

        [TestMethod()]
        public void When_Distinct_Process_ObjectListById_Return_UniqueValues()
        {
            var result = repeatedUsersList.Distinct((a, b) => a.Id == b.Id);
            Assert.AreEqual(3, result.Count());
        }
    }
}