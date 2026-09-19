# Changelog

## Unreleased

### 2.0.0

#### Changed

- Target .NET 10 / C# 14 and enable nullable annotations. Version 1.0.7 remains the final .NET Standard 2.0 legacy release.
- Replace `IfTrue()`, `IfFalse()`, and `IfNull()` with extension properties. Replace the `Operations` / `EmptyOperation` hierarchy with immutable `Condition` and `ValueCondition<T>` structs; default values are inactive.
- Replace preconstructed-exception `Throw` with lazy, type-safe exception factories and a parameterless generic overload. Required delegates are validated even for inactive conditions.
- Replace collection-only synchronous `Then` and `Alter` with generic `Tap` and `Pipe` in `FunctionalSharp.Composition`. Synchronous iteration consistently rejects null arguments.
- Require task-returning callbacks on `ThenAsync`, `AlterAsync`, `ForEveryAsync`, and `ForAsync`. Remove implicit `Task.Run` scheduling; callbacks are awaited sequentially. Optional cancellation tokens and token-aware delegates support cooperative cancellation.
- Preserve synchronous, zero-based `ForAsync` conditions and first-false stopping. Its combined asynchronous action/decision form returns `Task<bool>`.
- Add an optional matching hash delegate across all delegate-based LINQ methods. Equality-only calls use a correct constant-hash fallback, with a documented potential quadratic cost. Remove the `Distinct` constructor constraint and annotate nullable equality inputs/non-null dictionary keys.
- Replace `RepeatTimesOnFailure` / `repeatTimesOnFailure` with positive `MaxAttempts` / `maxAttempts`, including the initial attempt. Apply `StopOnFailure` after exhaustion; cancellation and observer exceptions do not enter the retry policy.
- Resolve data-reader property mappings once per result set. Require unique public writable instance properties, validate empty result schemas, and report conversion/setter failures with row/column/property context and the original cause. Conversions use invariant culture.
- Validate decorated-link names before construction, create only the requested type freshly per registration, and retain loadable types after partial assembly-load failures. Missing names use `KeyNotFoundException`; duplicates use `InvalidOperationException`.
- Mark reflection-based automatic mapping and decorated discovery as requiring unreferenced code. Trimming and Native AOT support are not claimed.

#### Added

- Typed `IfNotNull` callbacks for references and nullable values, and lazy `Match` result branches.
- Reusable `KeyComparer.By` for LINQ, sets, and dictionaries, with explicit null/key behavior.
- Explicit data-reader row mappers supporting immutable objects and custom constructors without reflection.
- Immutable synchronous/asynchronous pipelines with per-run payloads, explicit completed/stopped/cancelled/failed results, awaited async observers, and optional per-step retry policies.
- Immutable synchronous/asynchronous chains of responsibility with ordered first-match handlers, fallbacks, and explicit handled/unhandled/cancelled/failed results.
- Typed factory registries with duplicate-key validation, immutable snapshots, and caller-owned object lifetimes.
- Bounded `RetryPolicy` decorators with explicit retryable-failure predicates, total-attempt limits, and cancellation support.
- A migration guide, executable NuGet consumer examples, a checked CLR public API baseline, and reproducible allocation/throughput measurements against ordinary C#/LINQ.
- CI verification of the API baseline and packed consumer examples on Windows and Linux, retaining the direct-to-`master` workflow.

#### Fixed

- Delegate-based LINQ equality now produces correct set/group/join/dictionary/lookup results when equal values have different default hashes. Null equality delegates are rejected immediately.
- Async operations wait for suspended callbacks, propagate failures, and dispose iterators on completion, early stopping, failure, and cancellation. Running callbacks remain awaited after cancellation.
- Link-based chains configured to continue now advance after exhausted failures. Iterative retries have independent per-link/per-run budgets, and cancellation takes precedence over retrying or continuing.
- Decorated discovery excludes incompatible, abstract, open-generic, and non-constructible types before instantiation.
- Data-reader mapping supports nullable properties and retains assignments on struct rows. Database nulls map to actual nulls for reference/nullable properties and fail for non-nullable value properties.
- Pipelines retain a payload returned before cancellation is observed, consistently with or without retry wrapping.
- Replace ineffective callback-only assertions, invalid equality expectations, and incomplete reader fixtures with behavioral regression tests, including controlled async task gates and real multiple result sets.

## 1.0.7 - 2026-09-19

### Added

- GitHub Actions builds, tests, coverage reports, and package artifacts for pushes to `master` on Windows and Linux.
- A GitHub Release workflow that verifies the release and publishes to NuGet using trusted publishing.
- A packaged README, XML API documentation, and portable debugging symbols with Source Link metadata.
- Installation, local development, and maintainer release instructions.

### Changed

- Updated the test project to .NET 10 and refreshed its NuGet dependencies.
- Corrected package links to point to the FluentSharp repository.
- Made package creation an explicit `dotnet pack` step.

## 1.0.6 - 2021-06-16

- Previous published release. See [FluentSharp 1.0.6 on NuGet](https://www.nuget.org/packages/FluentSharp/1.0.6).
