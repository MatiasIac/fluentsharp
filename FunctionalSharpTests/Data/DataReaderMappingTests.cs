using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data;
using System.Data.Common;
using System.Globalization;

namespace FunctionalSharp.Data.Tests
{
    [TestClass]
    public class DataReaderMappingTests
    {
        [TestMethod]
        public void NullableProperties_MapConvertedValuesAndDatabaseNulls()
        {
            using var table = Table(
                new[] { ("Id", typeof(long)), ("Name", typeof(string)), ("Score", typeof(decimal)) },
                new object[] { 42L, "Ada", 12.5m },
                new object[] { DBNull.Value, DBNull.Value, DBNull.Value });
            using var reader = table.CreateDataReader();

            var rows = reader.ToList<NullableRow>();

            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual(42, rows[0].Id);
            Assert.AreEqual("Ada", rows[0].Name);
            Assert.AreEqual(12.5m, rows[0].Score);
            Assert.IsNull(rows[1].Id);
            Assert.IsNull(rows[1].Name);
            Assert.IsNull(rows[1].Score);
        }

        [TestMethod]
        public void StructRows_RetainAllPropertyAssignments_ForEachRow()
        {
            using var table = Table(
                new[] { ("Id", typeof(int)), ("Name", typeof(string)) },
                new object[] { 42, "Ada" }, new object[] { 43, "Grace" });
            using var reader = table.CreateDataReader();

            var rows = reader.ToList<StructRow>();

            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual(42, rows[0].Id);
            Assert.AreEqual("Ada", rows[0].Name);
            Assert.AreEqual(43, rows[1].Id);
            Assert.AreEqual("Grace", rows[1].Name);
        }

        [TestMethod]
        public void NullableStructProperty_MapsValuesAndNulls()
        {
            using var table = Table(new[] { ("Id", typeof(int)) },
                new object[] { 42 }, new object[] { DBNull.Value });
            using var reader = table.CreateDataReader();

            var rows = reader.ToList<NullableStructRow>();

            Assert.AreEqual(42, rows[0].Id);
            Assert.IsNull(rows[1].Id);
        }

        [TestMethod]
        public void DatabaseNull_MapsToNullReference_InsteadOfAnEmptyString()
        {
            using var table = Table(new[] { ("Name", typeof(string)) }, new object[] { DBNull.Value });
            using var reader = table.CreateDataReader();

            var row = reader.ToList<NamedRow>()[0];

            Assert.IsNull(row.Name);
        }

        [TestMethod]
        public void DatabaseNull_ForNonNullableProperty_ReportsColumnPropertyAndRow()
        {
            using var table = Table(new[] { ("Id", typeof(int)) }, new object[] { DBNull.Value });
            using var reader = table.CreateDataReader();

            var error = Assert.ThrowsExactly<DataException>(() => reader.ToList<PlainRow>());

            AssertMappingContext(error, "Id", nameof(PlainRow), 1);
            Assert.IsInstanceOfType<InvalidCastException>(error.InnerException);
            Assert.IsFalse(reader.IsClosed);
        }

        [TestMethod]
        public void InvalidConversion_ReportsTheFailingRow_AndPreservesItsCause()
        {
            using var table = Table(new[] { ("Id", typeof(string)) },
                new object[] { "42" }, new object[] { "invalid" });
            using var reader = table.CreateDataReader();

            var error = Assert.ThrowsExactly<DataException>(() => reader.ToList<PlainRow>());

            AssertMappingContext(error, "Id", nameof(PlainRow), 2);
            Assert.IsInstanceOfType<FormatException>(error.InnerException);
        }

        [TestMethod]
        public void UnknownColumn_ReportsSchemaError_BeforeReadingRows()
        {
            using var table = Table(new[] { ("Unknown", typeof(int)) }, new object[] { 42 });
            using var reader = table.CreateDataReader();

            var error = Assert.ThrowsExactly<DataException>(() => reader.ToList<PlainRow>());

            StringAssert.Contains(error.Message, "Unknown");
            StringAssert.Contains(error.Message, nameof(PlainRow));
            Assert.IsTrue(reader.Read());
            Assert.AreEqual(42, reader.GetInt32(0));
        }

        [TestMethod]
        public void ToMany_RebuildsMapping_ForTheSameTypeWithReorderedColumns()
        {
            using var firstTable = Table(new[] { ("Id", typeof(int)), ("Name", typeof(string)) },
                new object[] { 1, "first" });
            using var secondTable = Table(new[] { ("Name", typeof(string)), ("Id", typeof(long)) },
                new object[] { "second", 2L });
            using var reader = new DataTableReader(new[] { firstTable, secondTable });

            var (first, second) = reader.ToMany<PlainRow, PlainRow>();

            Assert.AreEqual(1, first[0].Id);
            Assert.AreEqual("first", first[0].Name);
            Assert.AreEqual(2, second[0].Id);
            Assert.AreEqual("second", second[0].Name);
            Assert.IsFalse(reader.IsClosed);
        }

        [TestMethod]
        public void ToMany_ThreeResults_MapsDifferentTypesIncludingStructs()
        {
            using var firstTable = Table(new[] { ("Id", typeof(int)) }, new object[] { 1 });
            using var secondTable = Table(new[] { ("Name", typeof(string)) }, new object[] { "second" });
            using var thirdTable = Table(new[] { ("Id", typeof(long)) }, new object[] { 3L });
            using var reader = new DataTableReader(new[] { firstTable, secondTable, thirdTable });

            var (first, second, third) = reader.ToMany<NullableRow, NamedRow, StructRow>();

            Assert.AreEqual(1, first[0].Id);
            Assert.AreEqual("second", second[0].Name);
            Assert.AreEqual(3, third[0].Id);
        }

        [TestMethod]
        public void ToMany_FourResults_PreservesEmptyResultsAndCaseOption()
        {
            using var firstTable = Table(new[] { ("id", typeof(int)) }, new object[] { 1 });
            using var secondTable = Table(new[] { ("name", typeof(string)) });
            using var thirdTable = Table(new[] { ("id", typeof(int)) }, new object[] { DBNull.Value });
            using var fourthTable = Table(new[] { ("id", typeof(int)) }, new object[] { 4 });
            using var reader = new DataTableReader(new[] { firstTable, secondTable, thirdTable, fourthTable });

            var (first, second, third, fourth) = reader.ToMany<PlainRow, NamedRow, NullableRow, StructRow>(ignoreCase: true);

            Assert.AreEqual(1, first[0].Id);
            Assert.AreEqual(0, second.Count);
            Assert.AreEqual(1, third.Count);
            Assert.IsNull(third[0].Id);
            Assert.AreEqual(4, fourth[0].Id);
            Assert.IsFalse(reader.IsClosed);
        }

        [TestMethod]
        public void ToMany_MissingLaterResults_ReturnsEmptyLists()
        {
            using var table = Table(new[] { ("Id", typeof(int)) }, new object[] { 1 });
            using var reader = table.CreateDataReader();

            var (first, second, third, fourth) = reader.ToMany<PlainRow, NamedRow, NullableRow, StructRow>();

            Assert.AreEqual(1, first.Count);
            Assert.AreEqual(0, second.Count);
            Assert.AreEqual(0, third.Count);
            Assert.AreEqual(0, fourth.Count);
            Assert.IsFalse(reader.IsClosed);
        }

        [TestMethod]
        public void ToList_LeavesTheNextResultAvailableToTheCaller()
        {
            using var firstTable = Table(new[] { ("Id", typeof(int)) }, new object[] { 1 });
            using var secondTable = Table(new[] { ("Name", typeof(string)) }, new object[] { "second" });
            using var reader = new DataTableReader(new[] { firstTable, secondTable });

            var first = reader.ToList<PlainRow>();

            Assert.AreEqual(1, first[0].Id);
            Assert.IsFalse(reader.IsClosed);
            Assert.IsTrue(reader.NextResult());
            Assert.AreEqual("second", reader.ToList<NamedRow>()[0].Name);
        }

        [TestMethod]
        public void NullReader_IsRejectedByAllMappingOverloads()
        {
            DbDataReader reader = null;

            var error = Assert.ThrowsExactly<ArgumentNullException>(() => reader.ToList<PlainRow>());
            Assert.AreEqual("reader", error.ParamName);
            Assert.ThrowsExactly<ArgumentNullException>(() => reader.ToMany<PlainRow, PlainRow>());
            Assert.ThrowsExactly<ArgumentNullException>(() => reader.ToMany<PlainRow, PlainRow, PlainRow>());
            Assert.ThrowsExactly<ArgumentNullException>(() => reader.ToMany<PlainRow, PlainRow, PlainRow, PlainRow>());
        }

        [TestMethod]
        public void PropertiesWithoutColumns_RetainTheirInitialValues()
        {
            using var table = Table(new[] { ("Id", typeof(int)) }, new object[] { 42 });
            using var reader = table.CreateDataReader();

            var row = reader.ToList<PlainRow>()[0];

            Assert.AreEqual(42, row.Id);
            Assert.AreEqual("initial", row.Name);
        }

        [TestMethod]
        public void PropertyNames_AreCaseSensitiveByDefault()
        {
            AssertInvalidProperty<PlainRow>("id");
        }

        [TestMethod]
        public void ReadOnlyProperties_AreRejected()
        {
            AssertInvalidProperty<ReadOnlyRow>("Id");
        }

        [TestMethod]
        public void PrivateSetters_AreRejected()
        {
            AssertInvalidProperty<PrivateSetterRow>("Id");
        }

        [TestMethod]
        public void Indexers_AreRejected()
        {
            AssertInvalidProperty<IndexerRow>("Item");
        }

        [TestMethod]
        public void StaticProperties_AreRejected_WithoutChangingTheirValue()
        {
            StaticPropertyRow.Id = 7;

            AssertInvalidProperty<StaticPropertyRow>("Id");

            Assert.AreEqual(7, StaticPropertyRow.Id);
        }

        [TestMethod]
        public void CaseInsensitiveMatch_RejectsAmbiguousProperties()
        {
            using var table = Table(new[] { ("Id", typeof(int)) }, new object[] { 42 });
            using var reader = table.CreateDataReader();

            var error = Assert.ThrowsExactly<DataException>(() => reader.ToList<CaseVariantRow>(ignoreCase: true));

            StringAssert.Contains(error.Message, "Id");
            StringAssert.Contains(error.Message, "multiple");
            StringAssert.Contains(error.Message, nameof(CaseVariantRow));
        }

        [TestMethod]
        public void CaseSensitiveMatch_CanMapDistinctCaseVariantProperties()
        {
            using var table = Table(new[] { ("Id", typeof(int)), ("id", typeof(int)) }, new object[] { 1, 2 });
            using var reader = table.CreateDataReader();

            var row = reader.ToList<CaseVariantRow>()[0];

            Assert.AreEqual(1, row.Id);
            Assert.AreEqual(2, row.id);
        }

        [TestMethod]
        public void ColumnsMappingToTheSameProperty_AreRejected()
        {
            using var table = Table(new[] { ("Id", typeof(int)), ("id", typeof(int)) }, new object[] { 1, 2 });
            using var reader = table.CreateDataReader();

            var error = Assert.ThrowsExactly<DataException>(() => reader.ToList<PlainRow>(ignoreCase: true));

            StringAssert.Contains(error.Message, "id");
            StringAssert.Contains(error.Message, "already mapped");
            StringAssert.Contains(error.Message, nameof(PlainRow));
        }

        [TestMethod]
        public void EmptyResults_StillValidateTheirSchema()
        {
            using var table = Table(new[] { ("Unknown", typeof(int)) });
            using var reader = table.CreateDataReader();

            Assert.ThrowsExactly<DataException>(() => reader.ToList<PlainRow>());
            Assert.IsFalse(reader.IsClosed);
        }

        [TestMethod]
        public void SetterFailure_PreservesTheOriginalException_WithMappingContext()
        {
            using var table = Table(new[] { ("Id", typeof(int)) }, new object[] { 42 });
            using var reader = table.CreateDataReader();

            var error = Assert.ThrowsExactly<DataException>(() => reader.ToList<ThrowingSetterRow>());

            AssertMappingContext(error, "Id", nameof(ThrowingSetterRow), 1);
            Assert.AreSame(ThrowingSetterRow.Failure, error.InnerException);
        }

        [TestMethod]
        public void NumericOverflow_IsReportedWithMappingContext()
        {
            using var table = Table(new[] { ("Id", typeof(long)) }, new object[] { long.MaxValue });
            using var reader = table.CreateDataReader();

            var error = Assert.ThrowsExactly<DataException>(() => reader.ToList<PlainRow>());

            AssertMappingContext(error, "Id", nameof(PlainRow), 1);
            Assert.IsInstanceOfType<OverflowException>(error.InnerException);
        }

        [TestMethod]
        public void StringConversions_UseInvariantCulture()
        {
            var previousCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
                using var table = Table(new[] { ("Score", typeof(string)) }, new object[] { "12.5" });
                using var reader = table.CreateDataReader();

                Assert.AreEqual(12.5m, reader.ToList<NullableRow>()[0].Score);
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void GuidAndEnumProperties_AcceptTypedValuesOrStrings(bool useStrings)
        {
            var id = Guid.Parse("727ecf70-7781-4e1f-bcba-66ae01f0bf48");
            using var table = Table(
                new[] { ("Id", typeof(object)), ("OptionalId", typeof(object)), ("State", typeof(object)), ("OptionalState", typeof(object)) },
                new object[] { useStrings ? (object)id.ToString() : id, useStrings ? (object)id.ToString() : id,
                    useStrings ? (object)"Ready" : State.Ready, DBNull.Value });
            using var reader = table.CreateDataReader();

            var row = reader.ToList<ConvertedRow>()[0];

            Assert.AreEqual(id, row.Id);
            Assert.AreEqual(id, row.OptionalId);
            Assert.AreEqual(State.Ready, row.State);
            Assert.IsNull(row.OptionalState);
        }

        [TestMethod]
        public void EnumProperties_AcceptUnderlyingNumbers_IncludingUndefinedValues()
        {
            using var table = Table(new[] { ("State", typeof(int)), ("OptionalState", typeof(int)) }, new object[] { 1, 99 });
            using var reader = table.CreateDataReader();

            var row = reader.ToList<ConvertedRow>()[0];

            Assert.AreEqual(State.Ready, row.State);
            Assert.AreEqual((State)99, row.OptionalState);
        }

        [TestMethod]
        [DataRow("State", "ready")]
        [DataRow("Id", "invalid-guid")]
        public void InvalidGuidOrEnumText_ReportsMappingContext(string column, string value)
        {
            using var table = Table(new[] { (column, typeof(string)) }, new object[] { value });
            using var reader = table.CreateDataReader();

            var error = Assert.ThrowsExactly<DataException>(() => reader.ToList<ConvertedRow>(ignoreCase: true));

            AssertMappingContext(error, column, nameof(ConvertedRow), 1);
            Assert.IsNotNull(error.InnerException);
        }

        [TestMethod]
        public void AssignableValues_AreMappedWithoutChangeTypeConversion()
        {
            var timestamp = new DateTimeOffset(2026, 9, 19, 12, 0, 0, TimeSpan.Zero);
            var bytes = new byte[] { 1, 2, 3 };
            using var table = Table(new[] { ("Timestamp", typeof(DateTimeOffset)), ("Bytes", typeof(byte[])) },
                new object[] { timestamp, bytes });
            using var reader = table.CreateDataReader();

            var row = reader.ToList<AssignableRow>()[0];

            Assert.AreEqual(timestamp, row.Timestamp);
            CollectionAssert.AreEqual(bytes, row.Bytes);
        }

        private static void AssertInvalidProperty<T>(string column) where T : new()
        {
            using var table = Table(new[] { (column, typeof(int)) }, new object[] { 42 });
            using var reader = table.CreateDataReader();

            var error = Assert.ThrowsExactly<DataException>(() => reader.ToList<T>());

            StringAssert.Contains(error.Message, column);
            StringAssert.Contains(error.Message, typeof(T).Name);
        }

        private static DataTable Table((string Name, Type Type)[] columns, params object[][] rows)
        {
            var table = new DataTable();
            foreach (var column in columns) table.Columns.Add(column.Name, column.Type);
            foreach (var row in rows) table.Rows.Add(row);
            return table;
        }

        private static void AssertMappingContext(DataException error, string column, string destination, int row)
        {
            StringAssert.Contains(error.Message, column);
            StringAssert.Contains(error.Message, destination);
            StringAssert.Contains(error.Message, $"row {row}");
        }

        public sealed class PlainRow
        {
            public int Id { get; set; }
            public string Name { get; set; } = "initial";
        }

        public sealed class NullableRow
        {
            public int? Id { get; set; } = -1;
            public string Name { get; set; } = "initial";
            public decimal? Score { get; set; } = -1;
        }

        public sealed class NamedRow
        {
            public string Name { get; set; } = "initial";
        }

        public struct StructRow
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public struct NullableStructRow
        {
            public int? Id { get; set; }
        }

        public sealed class ReadOnlyRow
        {
            public int Id => 0;
        }

        public sealed class PrivateSetterRow
        {
            public int Id { get; private set; }
        }

        public sealed class IndexerRow
        {
            public int this[int index] { get => 0; set { } }
        }

        public sealed class StaticPropertyRow
        {
            public static int Id { get; set; }
        }

        public sealed class CaseVariantRow
        {
            public int Id { get; set; }
            public int id { get; set; }
        }

        public sealed class ThrowingSetterRow
        {
            public static readonly Exception Failure = new InvalidOperationException("Setter failed.");
            public int Id { get => 0; set => throw Failure; }
        }

        public enum State { None, Ready }

        public sealed class ConvertedRow
        {
            public Guid Id { get; set; }
            public Guid? OptionalId { get; set; }
            public State State { get; set; }
            public State? OptionalState { get; set; }
        }

        public sealed class AssignableRow
        {
            public DateTimeOffset Timestamp { get; set; }
            public byte[] Bytes { get; set; }
        }
    }
}
