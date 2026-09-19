using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;

namespace FunctionalSharp.Data.Tests
{
    [TestClass]
    public class DataReadersTests
    {
        [TestMethod]
        public void When_ToList_ParseReader_GetListOfObjects()
        {
            using var table = CreateTable();
            using var reader = table.CreateDataReader();
            var result = reader.ToList<ResultType>();

            Assert.AreEqual(4, result.Count);
            Assert.AreEqual(10, result[0].Id);
            Assert.AreEqual(40, result[3].Id);
            Assert.AreEqual("Test 3", result[3].Name);
            Assert.IsFalse(reader.IsClosed);
        }

        [TestMethod]
        public void When_ToList_ParseReader_GetListOfObjectsIgnoringCase()
        {
            using var table = CreateTable();
            using var reader = table.CreateDataReader();
            var result = reader.ToList<LowerCaseResultType>(ignoreCase: true);

            Assert.AreEqual(4, result.Count);
            Assert.AreEqual(10, result[0].id);
            Assert.AreEqual(40, result[3].id);
            Assert.AreEqual("Test 3", result[3].name);
        }

        [TestMethod]
        public void When_ToList_ParseReaderToUpperCaseType_GetListOfObjectsIgnoringCase()
        {
            using var table = CreateTable(lowerCase: true);
            using var reader = table.CreateDataReader();
            var result = reader.ToList<ResultType>(ignoreCase: true);

            Assert.AreEqual(4, result.Count);
            Assert.AreEqual(10, result[0].Id);
            Assert.AreEqual(40, result[3].Id);
            Assert.AreEqual("Test 3", result[3].Name);
        }

        [TestMethod]
        public void When_ToMany_ParseReader_GetListOfSingleObjects()
        {
            using var table = CreateTable();
            using var reader = table.CreateDataReader();
            var (result, empty) = reader.ToMany<ResultType, EmptyType>();

            Assert.AreEqual(4, result.Count);
            Assert.AreEqual(10, result[0].Id);
            Assert.AreEqual(40, result[3].Id);
            Assert.AreEqual("Test 3", result[3].Name);
            Assert.AreEqual(0, empty.Count);
            Assert.IsFalse(reader.IsClosed);
        }

        private static DataTable CreateTable(bool lowerCase = false)
        {
            var table = new DataTable();
            table.Columns.Add(lowerCase ? "id" : "Id", typeof(int));
            table.Columns.Add(lowerCase ? "name" : "Name", typeof(string));
            table.Columns.Add(lowerCase ? "age" : "Age", typeof(int));
            table.Rows.Add(10, "Test", 20);
            table.Rows.Add(20, "Test 1", 55);
            table.Rows.Add(30, "Test 2", 32);
            table.Rows.Add(40, "Test 3", 78);
            return table;
        }

        public sealed class EmptyType { }

        public sealed class ResultType
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int Age { get; set; }
        }

        public sealed class LowerCaseResultType
        {
            public int id { get; set; }
            public string name { get; set; }
            public int age { get; set; }
        }
    }
}
