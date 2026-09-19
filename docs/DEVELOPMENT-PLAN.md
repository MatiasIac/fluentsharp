# FluentSharp 2 development plan

Prepared against version 1.0.7 on 2026-09-19, then revised to make version 2 an intentionally breaking redesign. Version 1.0.7 is the final legacy release. After accepting the first four correctness items, the maintainer authorized the remaining implementation as one batch, with review at the end. The status below describes the implementation; the numbered sections preserve the original design rationale and proposal history.

**Implementation progress**

- First review item, accepted by the maintainer: correct delegate-based LINQ equality. The implementation uses a constant-hash fallback, rejects null equality delegates, removes the unnecessary `Distinct` constructor constraint, and replaces the invalid inequality test. Regression tests cover set operations, all grouping overloads, joins, dictionaries, lookups, nulls, collisions, and deferred execution.
- Package version: `2.0.0`, targeting .NET 10 / C# 14 with nullable annotations and no runtime package dependencies.
- All delegate-based LINQ methods now accept optional matching hashes; `KeyComparer.By` supplies a reusable comparer for selected keys.
- First-item validation: 21 new regression cases failed before the fix. All 66 tests passed afterward, including 24 new tests; the LINQ extensions and comparer had full line/branch coverage in that run. Local NuGet and symbol packages built successfully as `2.0.0-alpha.1`.
- Second review item, accepted by the maintainer: chain failure/retry correctness and the discovery follow-up. `MaxAttempts` includes the initial attempt and must be positive; `StopOnFailure` applies after exhausted attempts. Retries use a loop. Cancellation takes precedence, cancellation exceptions propagate, and callback exceptions do not enter the retry policy. Existing chain tests assert outcomes outside callbacks, and the README includes migration examples.
- Second-item validation: four regression cases failed against the old implementation. All 93 tests now pass, including 27 new chain test cases. The configuration constructor, Run, and retry loop have full line/branch coverage in this run. Local NuGet and symbol packages build successfully as `2.0.0-alpha.2`.
- Discovery follow-up requested during review: confirmed the old TODO about incompatible decorated types. Discovery now filters by assignability to `LinkBase<T>`, excludes abstract/open-generic/non-constructible types, and reads each eligible type's decoration once before instantiation. Three initial regressions failed before the fix; all 103 tests pass afterward, including 10 new discovery cases covering filtering, shared names across payload types, and inherited decorations. The obsolete TODO is removed.
- Third review item, accepted by the maintainer: data-reader mapping correctness. Nullable and struct properties map correctly; database nulls have explicit rules; public writable instance properties are resolved once per result set. Schema validation rejects missing/ambiguous/duplicate matches. Conversion/setter errors include context. Real readers cover multiple, empty, and missing result sets, conversion rules, and ownership.
- Third-item validation: 10 initial regression cases failed against the old implementation. All 135 tests now pass, including 32 new mapping cases. Data-reader mapping has full line coverage in this run. Local NuGet and symbol packages build successfully as `2.0.0-alpha.3`.
- Fourth review item, accepted by the maintainer: async composition correctness. Task-returning delegates replace synchronous-action async overloads. Callbacks are awaited sequentially; every form supports optional cancellation and token-aware callbacks. `ForAsync` preserves synchronous conditions and first-false stopping, with `Task<bool>` for combined asynchronous work/decisions. Iterators are disposed on every exit path. Null arguments fail synchronously, execution failures propagate through tasks, and null callback tasks/results have explicit diagnostics. The README includes migration examples.
- Fourth-item validation: the initial suspended-callback regression failed against the old `ThenAsync`; it passes after the fix. All 188 tests pass, including 53 new async regression cases. Controlled task gates verify suspended callbacks, ordering, mixed completion, failures, and cancellation without timing-based sleeps. Local NuGet and symbol packages build successfully as `2.0.0-alpha.4`.
- Remaining-batch validation: Release build passes with warnings treated as errors; all 236 tests pass (48 added after the accepted async increment). The packed NuGet consumer compiles with nullable warnings treated as errors and runs successfully. The 275-line CLR API baseline verifies, NuGet/symbol packages build, and the benchmark harness produces recorded throughput/allocation results. Local execution was on Windows; CI is configured to repeat functional/package checks on Windows and Linux.
- Final-review regression: cancellation after a step returned a payload initially lost that payload when a retry policy wrapped the step. Both synchronous and asynchronous reproductions failed before the correction; pipelines now retain the same returned payload with or without retry wrapping.
- Status: the maintainer accepted the completed version 2 implementation and approved promotion from beta to the stable `2.0.0` version. Release publication remains a separate step. See the completion matrix below.

**Remaining implementation completion matrix**

| Plan area | Implemented decision and evidence |
| --- | --- |
| Target and property syntax | Single `net10.0` target, C# 14 extension properties, nullable annotations; verified through a packed-package consumer with warnings treated as errors. |
| Condition representation | Immutable `Condition` and typed `ValueCondition<T>` structs, default inactive, captured once. Lazy generic exception factories and parameterless construction; no runtime constructor matching or preconstructed-exception overload. |
| Value composition and branches | `Tap` / `Pipe` replace collection-only synchronous aliases. `Match` handles lazy result branches. Typed `IfNotNull` supports references and nullable value types. |
| Comparers | Optional matching hash delegates throughout the LINQ surface, plus `KeyComparer.By` for reuse with LINQ and hash collections. |
| Async composition | Sequential awaited collection callbacks and cancellation, already reviewed; new asynchronous patterns also await callbacks and observers. |
| Reusable pipelines | Immutable synchronous/asynchronous definitions, fresh per-run inputs, explicit outcomes, stop conditions, per-step retry, observers outside retry handling, and concurrent-run tests. |
| Chain of responsibility | Separate synchronous/asynchronous first-match handlers and fallbacks with handled/unhandled/cancelled/failed results. |
| Factory/strategy selection | Typed constructor registry with duplicate/unknown-key rules and immutable snapshots. Executable notifier example demonstrates selection; object ownership stays with the caller. |
| Retry decorators | Explicit failure predicates, bounded total attempts, synchronous/asynchronous execution, cooperative cancellation, and no cancellation retries. |
| Link discovery | Explicit `AddLink` registration remains available. Decorated lookup validates duplicates before construction, instantiates only requested types, and recovers loadable types after partial type-load errors. |
| Data mapping | Explicit row-mapper overload supports immutable models without reflection. Automatic reflection APIs are identified; no trimming/AOT compatibility claim. |
| Migration and release checks | [Migration guide](MIGRATING-TO-V2.md), [CLR API baseline](PUBLIC-API.txt), [packaged examples](../examples/Version2Consumer/Program.cs), and Windows/Linux CI checks. No publication is performed by this implementation work. |
| Measurements | Reproducible warmed throughput/allocation comparison of version 2 with ordinary C#/LINQ. After the version 2 review, the legacy preparation tool and development-only comparison cases were removed. See [method and results](PERFORMANCE.md). |

The original plan's explicitly deferred items remain deferred: parallel iteration, `ValueTask`, async streams, a DI container, command bus, singleton/observer/builder frameworks, and a trimming/Native AOT support claim. Runtime exception-constructor matching and generated forwarding overloads remain excluded. The new immutable pipelines intentionally stop on final failure; the existing object-link chain retains its separately documented stop/continue configuration. New factory builders are registration stages for the planned registry, not a general-purpose builder framework.

**Original design rationale (historical proposals; current decisions are above)**

**1. Product direction**

Make common C# operations easy to compose, with short expressions, useful IntelliSense, and predictable execution. Keep the library small, with no mandatory application framework, dependency-injection container, or runtime package dependencies in its core.

A useful fluent API should make the operation and its result clear. Prioritize compile-time safety, predictable behavior, and simple implementation over saving characters. Extra syntax is acceptable when it preserves constructor checking, debugging, and understandable failure behavior. Prefer a few consistent operations over many aliases for the same operation.

Preserve `FluentSharp` as the package name. Version 2 does not need source or binary compatibility with version 1.0.7: users can migrate their code or remain on the legacy package. Replace `IfTrue()` with `IfTrue` rather than carrying both spellings. Recommend .NET 10 / C# 14 as the version 2 baseline; .NET Standard 2.0 remains covered by version 1.0.7. Keep the solo-maintainer workflow: changes go to `master`, with the existing CI and explicit GitHub Releases.

**2. Decided exception API: `.IfTrue.Throw<TException>(() => new(...))`**

Use `Throw<TException>(Func<TException> exceptionFactory)` with `where TException : Exception`. This design is agreed for version 2; implementation remains a future task. Support both explicit and inferred exception types. The factory overload needs no `new()` constraint because the caller supplies construction. Runtime constructor matching and generated constructor-forwarding helpers are out of scope.

C# 14 introduces extension properties. An extension block can expose a conditional object as a property:

```csharp
// Proposed version 2 API, for C# 14 consumers.
public static class BooleanExtensions
{
    extension(bool value)
    {
        public Condition IfTrue => new(value);
        public Condition IfFalse => new(!value);
    }
}
```

This removes the parentheses at the call site. Reading the property still executes a getter; the syntax alone does not eliminate execution costs. [Microsoft: extension members](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/extension-methods)

There are two distinct parts to the desired syntax:

| Capability | Decision |
| --- | --- |
| `(a == b).IfTrue` | Supported by C# 14 extension properties; an isolated .NET 10 probe compiled and ran it. |
| `.Throw<InvalidOperationException>()` | Optional convenience overload to evaluate separately, using an `Exception, new()` constraint for a public parameterless constructor. |
| `.Throw(() => new CustomException(code, message))` | Accepted: type inference, compile-time constructor checking, and lazy construction. |
| `.Throw<CustomException>(() => new(code, message))` | Accepted: the generic type provides the target type for `new(...)`. Verified in the probe. |
| `.Throw<CustomException>(code, message)` for arbitrary constructor signatures | Excluded: runtime constructor binding adds infrastructure and loses constructor checking for the sake of shorter syntax. |

C#'s `new()` constraint describes a parameterless constructor. There is no general constraint for every possible parameterized constructor of an arbitrary exception type. Exception constructors also are not inherited. [Microsoft: constructor constraint](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/new-constraint)

Agreed spelling for the future implementation:

```csharp
// Proposed version 2, with the exception type inferred:
(a == b).IfTrue.Throw(() => new ArgumentException("Values must differ", nameof(a)));

// Alternatively, specify the exception type and use target-typed new:
(a == b).IfTrue.Throw<ArgumentException>(() => new("Values must differ", nameof(a)));
```

The factory is valuable beyond syntax: it creates the exception and computes its arguments only when the condition matches. Ordinary arguments in `.Throw<Exception>(BuildMessage())` are evaluated before the method runs, even when its condition is false.

Invoke the factory exactly once when the condition matches and never when it does not. Test that both the constructor and argument-building code stay unevaluated for an inactive condition. A normal delegate is sufficient; constructor reflection, expression-tree inspection, source generators, and custom factory interfaces are unnecessary for this API.

Structs and Moq clarify what is possible:

- A small `readonly struct` can carry a condition or typed fluent stage. A generic wrapper can expose callbacks and results using its type parameter. Its declared member signatures still determine what callers can pass; it cannot inherit arbitrary constructors from that type. Structs also cannot inherit from `Exception`. [Microsoft: structure types](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/struct)
- Moq's `Setup<TResult>` returns a typed setup using an expression tree. Its direct constructor-argument overload takes `params object[]` and matches a constructor at runtime. It also accepts `Expression<Func<T>>` containing a constructor call, which gives the compiler an actual constructor to check. These are distinct mechanisms. [Moq 4.20.72 source](https://github.com/devlooped/moq/blob/v4.20.72/src/Moq/Mock%601.cs)
- FluentSharp can use typed fluent stages, but throwing an exception only needs an executable `Func<TException>`; inspecting an expression tree adds no necessary behavior here.

Choose whether the condition itself is a struct using its semantics and measurements. That implementation choice is independent of the agreed factory-based exception API.

**3. Version 2 boundary and migration**

The earlier probe importing both the existing `FunctionalSharp.Validators` methods and a new property namespace produced compiler error **CS9339** for property access. Version 2 removes the old extension methods, so a parallel compatibility namespace is unnecessary.

Recommended approach:

- Leave version 1.0.7 as the final legacy release; plan all fixes and additions for version 2.
- Make `IfTrue`, `IfFalse`, and suitable null-condition properties the normal version 2 surface. Do not retain legacy aliases solely for compatibility.
- Recommend a single `net10.0` target and supported C# 14 language defaults. No .NET Standard compatibility target is required for version 2. Microsoft associates C# 14 with .NET 10 and documents using newer language versions than the target supports as unsupported. [Microsoft: language versioning](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-versioning)
- Review namespaces, public types, and overloads for clarity. Retain familiar names where useful, without allowing compatibility concerns to dictate the design.
- Provide a migration guide covering property syntax, async delegates, retry semantics, removed APIs, and any namespace changes.
- Verify cross-assembly use through actual packaged version 2 consumer projects. The initial syntax probe is a feasibility check, not a complete validation matrix.

Settle the public API through version 2 prereleases before publishing 2.0.0. Incremental review builds use a prerelease version; the full version 2 API is not yet implemented.

**4. Phase one: establish correct behavior and meaningful tests**

The existing 42 passing tests are a useful starting point. They do not cover several behaviors that matter to consumers. Isolated probes against the current implementation reproduced the following:

| Area | Confirmed issue | Planned work and acceptance criteria |
| --- | --- | --- |
| LINQ equality | Two objects considered equal by the supplied delegate remain distinct when their hash codes differ. | Add explicit equality-plus-hash overloads where needed and a reusable key-based comparer. Fix equality-only hashing using a correctness-preserving fallback, such as a constant hash, and document its potential quadratic cost. Verify set, grouping, joining, dictionary, lookup, null, and collision behavior. |
| Async callbacks | `ForEveryAsync(async item => ...)` returns before a deliberately suspended callback completes. The callback binds to `Action<T>`. | Add task-returning delegate APIs that await each callback. Verify suspended work, exceptions, ordering, and cancellation. Do not silently treat a synchronous delegate as asynchronous I/O. |
| Chain failures | `stopOnFailure: false` with the default zero repeat count still stops after an exception. | Specify the version 2 stop/continue/retry matrix, then implement it with regression tests and document migration from the old behavior. |
| Chain tests | The cancellation test puts its assertion inside `OnCompleted`, but cancellation suppresses that callback. The assertion never runs. | Assert payload, callback counts, and result state outside callbacks. Verify that every expected callback actually executes. |
| Data mapping | A numeric column mapped to `int?` throws `InvalidCastException`. A struct row's mapped property remains its default value because updates affect a boxed copy. | Implement explicit nullable and value-type handling. Add real `DataTableReader` tests with multiple schemas and result sets. |

Equal values must have equal hash codes under an equality comparer. An arbitrary comparison delegate cannot supply a correct hash automatically. [Microsoft: equality/hash contract](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.iequalitycomparer-1.gethashcode)

Additional findings from source inspection:

- The LINQ test using `(a, b) => a != b` as equality encodes an invalid equality relation. Replace that expectation with valid comparer cases.
- The third correctness item replaces per-row property discovery with a mapping local to each result set. It defines handling of `DBNull`, missing columns, read-only/indexed/static properties, enums, `Guid`, duplicate matches, and case collisions, and reports conversion errors with context. No global destination-type-only mapping cache is used.
- Named chain-link discovery now filters incompatible and non-constructible types before instantiation. It still scans loaded assemblies and eagerly constructs eligible links. A later pattern-design review can evaluate explicit registration, duplicate-name diagnostics before construction, and partial type-load failures.
- The operation factory allocates a new active or inactive object on each condition. Compare a small immutable condition struct with shared stateless implementations; choose using semantics and allocation measurements, without preserving the old public hierarchy solely for compatibility.
- Remove the unnecessary `new()` constraint on the explicit-hash `Distinct` overload if that overload remains in version 2.
- Standardize null argument diagnostics. Validate required delegates at the boundary without executing inactive callbacks, and document the version 2 inactive/null behavior.

Make correctness the first version 2 milestone. Record regressions against version 1.0.7, then carry the behavioral scenarios into the new API's tests. Version 2 can deliberately redefine retry counts and other contracts; explain each migration-relevant change. Do not publish a further version 1 release as part of this plan.

**5. Phase two: a small, consistent fluent core**

Start with three concepts: conditionally execute, transform a value, and observe a value while retaining it.

| Proposed API | Contract |
| --- | --- |
| `Throw<TException>(Func<TException>)` | Agreed: invoke the factory once and throw its exception only for a matching condition; allow type inference. Evaluate an existing-exception overload on its own usefulness, rather than for compatibility. |
| `Throw<TException>()` | Use the public parameterless constructor, only for a matching condition. |
| `IfNotNull` / typed conditional operations | Preserve the original type for callbacks rather than forcing consumers to recover it from an object. |
| `Pipe<T, TResult>(Func<T, TResult>)` | Transform any value and return the result, with ordinary type inference. |
| `Tap<T>(Action<T>)` | Execute a synchronous action and return the original value. Make immediate execution explicit. |
| Typed `Match` or `Otherwise` | Support branch results/fallbacks only where they improve real examples; define which branches execute and preserve laziness. |

Proposed composition example:

```csharp
var receipt = order
    .Tap(ValidateOrder)
    .Pipe(CalculateTotal)
    .Pipe(CreateReceipt);
```

Choose one consistent vocabulary, including whether `Then` and `Alter` still earn a place. Avoid adding several synonyms for each concept. If one method name accepts both action and transformation delegates, test overload resolution: expression lambdas can be convertible to multiple delegate shapes. `Pipe` and `Tap` have clear separate roles.

A small immutable condition struct is a reasonable replacement for the `Operations` hierarchy. Version 2 can change return types and remove subclassing if that produces a simpler API. Specify default-value behavior and avoid boxing or unnecessary copying; measure the result before making performance claims.

Modern .NET already has useful guard and sequence APIs, including [ArgumentNullException.ThrowIfNull](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception.throwifnull) and [Enumerable.DistinctBy](https://learn.microsoft.com/en-us/dotnet/api/system.linq.enumerable.distinctby). Use them as behavioral references. Do not add conflicting copies under broadly imported namespaces. A key-based comparer remains useful across multiple LINQ operations.

Acceptance: examples compile with their documented imports; inactive factories have no side effects; conditions are evaluated once unless explicitly supplied as reevaluated delegates; return types remain useful for the next operation; null and failure behavior is documented.

**6. Phase three: real asynchronous composition**

Design around `Func<T, Task>`, `Func<T, Task<TResult>>`, and cancellation-aware variants. Await sequentially by default, preserve input order, and propagate failures through the returned task.

```csharp
// Implemented in the fourth review item; callbacks return tasks and are awaited.
await orders.ForEveryAsync((order, token) => SaveAsync(order, token), cancellationToken);
```

Keep parallel processing as a separately named operation with an explicit concurrency limit, added only if there is demand. Define whether results preserve order and how faults/cancellation are reported before implementation.

The fourth review item removes `Task.Run` and the old synchronous-action async overloads, documents migration, and verifies both `async` lambdas and task-returning method groups. Required arguments are validated synchronously. Cancellation is checked before enumeration and callbacks, between items, and after callbacks. A running callback remains awaited even when cancellation is requested. `ThenAsync` and `AlterAsync` do not enumerate their input/output; iteration methods dispose their enumerators on every exit path.

Prefer `Task` first. Add `ValueTask` only when measurements justify the extra surface. Consider async streams later when there are concrete use cases. Cancellation is a control outcome, not a retryable exception by default.

Acceptance: use controlled incomplete tasks in tests rather than sleeps; prove the public task stays incomplete until the callback completes. Check exceptions, cancellation between items, empty inputs, and mixed synchronous/asynchronous completion. Keep synchronous collection operations immediate; distinguish them from deferred LINQ queries.

**7. Phase four: finish and distinguish the patterns**

The current `GenericChain<T>` normally executes every link. That is primarily a sequential processing pipeline. A chain of responsibility often stops when a handler accepts a request. Both are useful, but their completion rules should be explicit.

| Candidate | Priority and scope |
| --- | --- |
| Reusable processing pipeline | First. Define ordered steps once, then execute with a fresh payload/context per run. Provide synchronous and asynchronous execution and an explicit result. |
| Chain of responsibility | Next. Ordered predicates/handlers with an explicit handled/not-handled outcome, first-match behavior, and a fallback. A handled request is different from cancellation or failure. |
| Typed factory/strategy selection | Next if real examples justify it. Map typed keys to constructor delegates and return an immutable factory; validate duplicate/unknown keys. |
| Retry/decorator composition | Later. Apply a bounded retry policy around an operation. Explicitly identify retryable failures and do not retry cancellation. |
| Builder, singleton, observer, command bus, DI container | Defer. C# object construction, delegates, events, and Lazy<T> already cover many simple cases. Require concrete examples of saved complexity first. |

For a new pipeline, separate its immutable definition from each execution's mutable context. Return a result distinguishing completed, stopped, cancelled, and failed states, with payload and relevant error information. Callbacks supplement that result. Define reruns and concurrent execution without implying that a mutable payload or user-supplied handler becomes thread-safe.

The second correctness item already replaces recursive retry execution with an iterative loop and introduces `MaxAttempts` (including the first attempt). Preserve this contract in the later pipeline redesign. The README documents migration from legacy configuration. Callback failures propagate directly and do not enter the retry policy.

Proposed factory example:

```csharp
var factory = Factory.For<NotificationChannel, INotifier>()
    .Register(NotificationChannel.Email, () => new EmailNotifier(mailClient))
    .Register(NotificationChannel.Sms, () => new SmsNotifier(smsClient))
    .Build();

var notifier = factory.Create(channel);
```

This is a typed factory registry/strategy selector, rather than the inheritance-based GoF Factory Method pattern. Describe that distinction accurately in the documentation. Keep lifecycle, caching, disposal, and dependency injection under the caller's control. A helper that merely wraps one `Func<T>` without adding useful behavior does not merit a new subsystem.

Use explicit row-mapping delegates as the first data-reader extension beyond correctness fixes. They cover custom types without expanding this library into an ORM. Automatic mapping can remain a small convenience with documented conversion rules and appropriate reflection/trimming checks.

**8. Phase five: measurement and version 2 release readiness**

Design the C# 14 property surface as part of the version 2 core, then validate its behavior and consumer experience before release.

Before publishing:

- Compile version 2 consumer examples with their documented imports. Include custom exception constructors, method groups, null literals, and lambdas in overload-resolution checks.
- Review the public API differences from version 1.0.7 to build an accurate migration guide; those differences are expected, not compatibility failures. Establish a version 2 baseline for future compatible releases.
- Benchmark representative conditional, collection, comparer, and mapping workloads against the old implementation and ordinary C#/LINQ. Measure allocations as well as throughput. No performance claims based solely on shorter syntax.
- Test disposal and one-shot/lazy sequences, and actual multiple data-reader result sets. Validate nullable annotations on any modern APIs rather than promising that a chained null check teaches the compiler about an unrelated variable.
- If advertising trimming or Native AOT support, add real publish-and-run checks. Avoid unconditional assembly scanning and runtime constructor binding on paths intended to support those deployment modes.
- Publish complete examples and migration notes explaining the problem each helper solves, its evaluation timing, and its cost. Use version 2 prerelease packages before declaring its naming stable. Clearly identify version 1.0.7 as the final legacy option.

No new contributor workflow, branch policy, or package fragmentation is needed for this plan. Add optional tooling packages only if their user benefit is demonstrated.

**9. Suggested first version 2 implementation batch**

1. Finalize the version 2 target and remaining public API examples, using the agreed property syntax and lazy exception factory.
2. Replace callback-only chain assertions and the invalid LINQ equality test with meaningful behavioral tests.
3. Correct comparer hashing, chain continuation/retry rules, and nullable/struct data-reader mapping under documented version 2 contracts.
4. Build the property-based conditional API, lazy exception construction, and task-returning async delegates; remove superseded APIs and validate consumer examples.
5. Review a focused version 2 prerelease with migration notes before expanding the pattern APIs or publishing 2.0.0.

The later phases are candidates, not a commitment to implement every feature. Each addition should demonstrate a useful fluent call site, a small implementation, and a clear behavior contract before it joins the public API.
