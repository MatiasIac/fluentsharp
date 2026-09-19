# Version 2 measurements

These are local microbenchmarks for choosing implementations and explaining costs, not general performance guarantees. Shorter syntax alone says nothing about throughput. Run on representative application inputs before making performance decisions.

## Reproduce

From the repository root with the .NET 10 SDK:

```shell
dotnet run --project benchmarks/FluentSharp.Benchmarks.csproj --configuration Release -- artifacts/benchmarks/results.csv
```

The benchmark project references the current library and compares it with ordinary C#/LINQ. It runs directly from a checkout without preparing another project or reading git history.

The dependency-free harness warms each scenario with 2,000 calls, then measures five samples and reports the median nanoseconds and allocated bytes per operation. Release builds disable tiered compilation for this harness. Allocation counts use `GC.GetAllocatedBytesForCurrentThread`; workloads are synchronous, results feed a checksum, and input preparation is excluded. Enumeration, mapping output allocation, and reader creation are included. Timings include the measurement delegate's invocation overhead. Results are not CI pass/fail thresholds.

## Recorded run

2026-09-19, Windows x64 build 26200, SDK 10.0.401, runtime .NET 10.0.12. Raw results are in [windows-net10.csv](../benchmarks/results/windows-net10.csv).

| Scenario | Median ns/op | Median bytes/op |
| --- | ---: | ---: |
| Condition: ordinary `if` | 2.00 | 0 |
| Condition: version 2 struct | 2.68 | 0 |
| Collection: ordinary `foreach` | 715.43 | 32 |
| Collection: version 2 `ForEvery` | 1,063.81 | 120 |
| Comparer: ordinary LINQ default | 3,683.65 | 8,552 |
| Comparer: version 2 constant hash | 168,007.25 | 8,584 |
| Comparer: version 2 explicit hash | 5,118.25 | 8,584 |
| Comparer: version 2 key comparer | 6,821.85 | 8,552 |
| Mapping: manual loop | 10,419.70 | 13,416 |
| Mapping: version 2 reflection | 21,071.00 | 13,845.22 |
| Mapping: version 2 explicit mapper | 10,154.90 | 13,416 |

Condition scenarios alternate true/false with a reused action and one million operations per sample. Both the ordinary branch and version 2 condition struct avoid allocations here. Capturing lambdas and user callbacks can still allocate.

Collection scenarios visit 256 integers through `IEnumerable<int>`, 20,000 times per sample. The closure/action required by the fluent loop costs more than the ordinary loop in this harness.

Comparer scenarios deduplicate 512 integers containing 256 distinct keys, 2,000 times per sample. Integer equality agrees with the default hash, so all variants have equal result checksums. The constant hash is correct for arbitrary valid equality delegates but is visibly expensive here; use an explicit matching hash, key comparer, or standard comparer for growing inputs.

Mapping scenarios read 100 three-column rows into lists, 1,000 times per sample. Automatic mapping resolves the schema once per result set. Explicit mapping approaches the manual loop and avoids reflection. These figures include an in-memory `DataTableReader`, not database/network latency or all provider behaviors.

There are no cross-platform performance, trimming, or Native AOT claims from this run. Rerun the harness after relevant changes and preserve the environment and raw results when comparing runs.
