using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using FunctionalSharp.Collections;
using FunctionalSharp.Data;
using FunctionalSharp.Linq;
using FunctionalSharp.Validators;

var rows = new List<string> { "scenario,median_ns_per_op,median_bytes_per_op,checksum" };
var counter = 0;
var tick = 0;
Action action = () => counter++;
Measure("condition/ordinary-if", () => { if ((tick++ & 1) == 0) action(); return counter; }, 1_000_000);
Measure("condition/v2-struct", () => { ((tick++ & 1) == 0).IfTrue.Then(action); return counter; }, 1_000_000);

IEnumerable<int> values = Enumerable.Range(1, 256).ToArray();
Measure("collection/foreach", () => { var sum = 0; foreach (var item in values) sum += item; return sum; }, 20_000);
Measure("collection/v2-ForEvery", () => { var sum = 0; values.ForEvery(item => sum += item); return sum; }, 20_000);

var duplicates = values.Concat(values).ToArray();
Func<int, int, bool> equal = (left, right) => left == right;
var byKey = KeyComparer.By((int value) => value);
Measure("comparer/LINQ-default", () => duplicates.Distinct().Count(), 2_000);
Measure("comparer/v2-constant-hash", () => duplicates.Distinct(equal).Count(), 2_000);
Measure("comparer/v2-explicit-hash", () => duplicates.Distinct(equal, value => value.GetHashCode()).Count(), 2_000);
Measure("comparer/v2-key", () => duplicates.Distinct(byKey).Count(), 2_000);

using var table = new DataTable();
table.Columns.Add("Id", typeof(int));
table.Columns.Add("Name", typeof(string));
table.Columns.Add("Total", typeof(decimal));
for (var index = 0; index < 100; index++) table.Rows.Add(index, "row", (decimal)index);
Measure("mapping/manual", () =>
{
    using var reader = table.CreateDataReader();
    var mapped = new List<Row>();
    while (reader.Read()) mapped.Add(new Row { Id = reader.GetInt32(0), Name = reader.GetString(1), Total = reader.GetDecimal(2) });
    return mapped.Sum(row => row.Id);
}, 1_000);
Measure("mapping/v2-reflection", () => { using var reader = table.CreateDataReader(); return reader.ToList<Row>().Sum(row => row.Id); }, 1_000);
Measure("mapping/v2-explicit", () =>
{
    using var reader = table.CreateDataReader();
    return reader.ToList(row => new Row { Id = row.GetInt32(0), Name = row.GetString(1), Total = row.GetDecimal(2) }).Sum(row => row.Id);
}, 1_000);

var output = args.Length > 0 ? args[0] : "artifacts/benchmarks/results.csv";
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
File.WriteAllLines(output, rows);
Console.WriteLine($"{RuntimeInformation.FrameworkDescription}; {RuntimeInformation.OSDescription}; {RuntimeInformation.ProcessArchitecture}");
Console.WriteLine($"Wrote {output}. Each row is the median of 5 warmed samples; tiered compilation is disabled.");

void Measure(string name, Func<long> operation, int iterations)
{
    counter = 0;
    tick = 0;
    for (var warmup = 0; warmup < 2_000; warmup++) operation();
    var times = new double[5];
    var allocations = new double[5];
    long checksum = 0;
    for (var sample = 0; sample < times.Length; sample++)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        var start = Stopwatch.GetTimestamp();
        long total = 0;
        for (var iteration = 0; iteration < iterations; iteration++) total += operation();
        var elapsed = Stopwatch.GetTimestamp() - start;
        allocations[sample] = (GC.GetAllocatedBytesForCurrentThread() - bytes) / (double)iterations;
        times[sample] = elapsed * (1_000_000_000d / Stopwatch.Frequency) / iterations;
        checksum = total;
    }
    Array.Sort(times);
    Array.Sort(allocations);
    var line = string.Create(CultureInfo.InvariantCulture, $"{name},{times[2]:F2},{allocations[2]:F2},{checksum}");
    rows.Add(line);
    Console.WriteLine(line);
}

public sealed class Row
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Total { get; set; }
}
