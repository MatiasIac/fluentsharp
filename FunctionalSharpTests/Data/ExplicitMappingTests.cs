#nullable enable
using System;
using System.Data;
using System.Data.Common;
using FunctionalSharp.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FunctionalSharp.Tests;

[TestClass]
public class ExplicitMappingTests
{
    [TestMethod]
    public void ExplicitMapper_SupportsImmutableRecords_AndLeavesNextResultForCaller()
    {
        using var first = Table(3, 5);
        using var second = Table(8);
        using var reader = new DataTableReader([first, second]);
        var calls = 0;
        var rows = reader.ToList(row => { calls++; return new Row(row.GetInt32(0)); });
        Assert.AreEqual(2, calls);
        Assert.AreEqual(3, rows[0].Id);
        Assert.AreEqual(5, rows[1].Id);
        Assert.IsFalse(reader.IsClosed);
        Assert.IsTrue(reader.NextResult());
        Assert.AreEqual(8, reader.ToList(row => new Row(row.GetInt32(0)))[0].Id);
    }

    [TestMethod]
    public void ExplicitMapper_PropagatesOriginalException_WithoutDisposingReader()
    {
        using var table = Table(3, 5);
        using var reader = table.CreateDataReader();
        var failure = new InvalidOperationException();
        Assert.AreSame(failure, Assert.ThrowsExactly<InvalidOperationException>(() => reader.ToList<Row>(row => throw failure)));
        Assert.IsFalse(reader.IsClosed);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(5, reader.GetInt32(0));
    }

    [TestMethod]
    public void EmptyInput_SkipsMapper_ButRejectsNullArguments()
    {
        using var table = Table();
        using var reader = table.CreateDataReader();
        Assert.ThrowsExactly<ArgumentNullException>(() => reader.ToList<Row>(null!));
        Assert.AreEqual(0, reader.ToList<Row>(row => throw new Exception()).Count);
        Assert.ThrowsExactly<ArgumentNullException>(() => ((DbDataReader)null!).ToList(row => 1));
    }

    private static DataTable Table(params int[] values)
    {
        var table = new DataTable();
        table.Columns.Add("identifier", typeof(int));
        foreach (var value in values) table.Rows.Add(value);
        return table;
    }

    private sealed record Row(int Id);
}
