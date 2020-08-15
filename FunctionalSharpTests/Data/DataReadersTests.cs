using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;

namespace FunctionalSharp.Data.Tests
{
    [TestClass()]
    public class DataReadersTests
    {
        private List<List<(string, Object)>> data;

        [TestInitialize()]
        public void Setup()
        {
            data = new List<List<(string, Object)>>
            {
                new List<(string, Object)> { ("Id", 10), ("Name", "Test"), ("Age", 20) },
                new List<(string, Object)> { ("Id", 20), ("Name", "Test 1"), ("Age", 55) },
                new List<(string, Object)> { ("Id", 30), ("Name", "Test 2"), ("Age", 32) },
                new List<(string, Object)> { ("Id", 40), ("Name", "Test 3"), ("Age", 78) }
            };
        }

        [TestMethod()]
        public void When_ToList_ParseReader_GetListOfObjects()
        {
            var datareader = new CustomReader(data);
            var result = datareader.ToList<ResultType>();

            Assert.AreEqual(4, result.Count);
            Assert.AreEqual(10, result[0].Id);
            Assert.AreEqual(40, result[3].Id);
            Assert.AreEqual("Test 3", result[3].Name);
        }

        [TestMethod()]
        public void When_ToMany_ParseReader_GetListOfSingleObjects()
        {
            var datareader = new CustomReader(data);
            var (result, emptyType) = datareader.ToMany<ResultType, EmptyType>();

            Assert.AreEqual(4, result.Count);
            Assert.AreEqual(10, result[0].Id);
            Assert.AreEqual(40, result[3].Id);
            Assert.AreEqual("Test 3", result[3].Name);
            Assert.AreEqual(0, emptyType.Count);
        }
    }

    internal class EmptyType
    {

    }

    internal class ResultType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
    }

    internal class CustomReader : DbDataReader
    {
        private List<List<(string, Object)>> _dataCollection;
        private int dataIndex = -1;

        public CustomReader(List<List<(string, Object)>> dataCollection)
        {
            _dataCollection = dataCollection;
        }

        #region not implemented
        public override object this[string name] => throw new NotImplementedException();

        public override int Depth => throw new NotImplementedException();

        public override bool HasRows => throw new NotImplementedException();

        public override bool IsClosed => throw new NotImplementedException();

        public override int RecordsAffected => throw new NotImplementedException();

        public override bool GetBoolean(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override byte GetByte(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
        {
            throw new NotImplementedException();
        }

        public override char GetChar(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
        {
            throw new NotImplementedException();
        }

        public override string GetDataTypeName(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override DateTime GetDateTime(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override decimal GetDecimal(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override double GetDouble(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public override float GetFloat(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override Guid GetGuid(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override short GetInt16(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override int GetInt32(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override long GetInt64(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override int GetOrdinal(string name)
        {
            throw new NotImplementedException();
        }

        public override string GetString(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override object GetValue(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override int GetValues(object[] values)
        {
            throw new NotImplementedException();
        }

        public override bool IsDBNull(int ordinal)
        {
            throw new NotImplementedException();
        }
        #endregion

        public override object this[int ordinal] => _dataCollection[dataIndex][ordinal].Item2;

        public override string GetName(int ordinal)
        {
            return _dataCollection[dataIndex][ordinal].Item1;
        }

        public override int FieldCount => _dataCollection[dataIndex].Count;

        public override bool NextResult()
        {
            return false;
        }

        public override Type GetFieldType(int ordinal)
        {
            return _dataCollection[dataIndex][ordinal].Item2.GetType();
        }

        public override bool Read()
        {
            return ++dataIndex < _dataCollection.Count;
        }
    }
}