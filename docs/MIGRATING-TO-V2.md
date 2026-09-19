# Migrating from FluentSharp 1.0.7 to version 2

Version 1.0.7 is the final legacy release. Version 2 targets .NET 10 and C# 14, with no source or binary compatibility promise for version 1. Keep 1.0.7 for .NET Standard/.NET Framework applications. The package remains `FluentSharp`; assembly and namespaces remain `FunctionalSharp`.

The current checkout is prepared for the stable `2.0.0` release. Publishing remains the maintainer's explicit GitHub Release action. Recompile consumers when upgrading.

## Conditions and exceptions

| Version 1 | Version 2 |
| --- | --- |
| `condition.IfTrue()` / `.IfFalse()` | `condition.IfTrue` / `.IfFalse` |
| `value.IfNull()` | `value.IfNull` |
| `.Throw(new CustomException(code, BuildMessage()))` | `.Throw<CustomException>(() => new(code, BuildMessage()))` |
| `Operations` / `EmptyOperation` classes | Immutable `Condition` / `ValueCondition<T>` structs |
| Collection `.Then(action)` | `.Tap(action)` with `using FunctionalSharp.Composition;` |
| Collection `.Alter(transform)` | `.Pipe(transform)` with `using FunctionalSharp.Composition;` |

`Condition.Then(Action)` remains the conditional action method. It returns the same captured condition. Reading a property captures the current value once; later variable assignments do not change the condition. A default condition is inactive. Required delegates are validated immediately even on inactive stages. `Then`'s delegate parameter is named `action`.

`Throw` now takes a factory rather than an already-created exception. Constructor arguments and side effects inside the factory run only on a match. Generic inference works with `Throw(() => new CustomException(...))`; explicit type arguments permit target-typed `new(...)`. `Throw<TException>()` uses a public parameterless constructor lazily. A matched factory returning null throws `InvalidOperationException`; exceptions from factories propagate unchanged.

`IfNull` and `IfNotNull` support reference types and nullable value types. A non-nullable value type needs no null check. An untyped `null` literal has no receiver type; use a typed variable or cast, for example `((string?)null).IfNull`. `IfNotNull.Then` and `IfNotNull.Match` receive the original non-null type (or the underlying nullable value type). They do not change the compiler's null-state analysis for the original variable.

Use `Match` for lazy results from two branches. Both branch delegates are required, but exactly one runs. `Pipe` and `Tap` execute immediately, preserve normal generic inference, allow null values through, and do not enumerate sequences themselves. They replace the two collection-specific synchronous aliases. Callback errors propagate unchanged. Do not pass `async void` lambdas to synchronous action APIs.

## Async collection operations

`ThenAsync`, `AlterAsync`, `ForEveryAsync`, and `ForAsync` require task-returning callbacks. Their old synchronous callback overloads and implicit `Task.Run` scheduling are removed. `ThenAsync`/`AlterAsync` retain their sequence-specific names; `Tap`/`Pipe` cover synchronous values of any type.

```csharp
await orders.ForEveryAsync((order, token) => SaveAsync(order, token), cancellationToken);
await orders.ForAsync(order => order.IsReady,
    (order, token) => SaveAsync(order, token), cancellationToken);
```

Callbacks are awaited sequentially, before reading the next item. `ForAsync` conditions are synchronous, zero-based when indexed, and stop before the first false condition. The combined action/decision form returns `Task<bool>` and stops after its first false callback result. Conditions are not filters that resume on a later match.

Optional cancellation tokens control execution; token-aware delegates also receive the token. Cancellation is cooperative, and running callbacks remain awaited. Callback, condition, and enumeration failures propagate through the returned task. Null required arguments throw synchronously. Null returned tasks or null `AlterAsync` results fault the task with `InvalidOperationException`. Collection iterators are disposed on every exit path.

## LINQ equality

Delegate-based equality now uses a constant hash when no hash function is supplied. This fixes incorrect set/group/join results when equal values have different default hashes, at a possible quadratic cost. All delegate-based operations accept an optional final `hashCodeComparer`; equal values must have equal hashes. Equality callbacks receive nullable arguments in the annotations and must handle any nulls their input permits. `ToDictionary` annotates its key type as `notnull`.

`Distinct` no longer requires `new()`. The delegate overloads have changed CLR signatures, so rebuild consumers even when their source calls stay unchanged. Direct method-group assignments to these extension methods may need a lambda to supply the new optional hash argument; equality/hash callback method groups remain supported. Null equality callbacks are rejected immediately. Standard LINQ rules still control null keys, lazy enumeration, duplicate dictionary keys, and which operations use hashing.

Prefer `KeyComparer.By((User user) => user.Id)` when equality comes from a key. It can be reused with standard LINQ, `HashSet<T>`, and `Dictionary<TKey,TValue>`. Null source values compare only with other null sources; the selector is not called for them. Null keys hash to zero. Do not mutate keys while using a hash collection.

## Data readers

Automatic mapping now supports nullable properties and struct rows. `DBNull` becomes null for reference/nullable properties and fails for non-nullable value properties; strings no longer silently become empty strings. Nullable reference annotations are not runtime schema validation.

Every column must uniquely match a public writable non-indexed instance property, including for empty results. Duplicate assignments, ambiguous case-insensitive matches, and unknown columns fail before reading rows. Properties absent from the selected columns keep their defaults. Mapping plans are resolved once per result set.

Value conversions use invariant culture, retain assignable values, and support nullable underlying types, GUID strings, and enum names/numbers. Conversion/setter failures now report `DataException` with row/column/property context and the original cause. Reader and row-constructor errors propagate. `ToMany` still returns empty lists for missing results. Readers remain caller-owned on success and failure.

Use `reader.ToList(row => new Receipt(row.GetInt32(0), row.GetDecimal(1)))` for custom/immutable objects without reflection or constructor constraints. The mapper must not advance or dispose the reader. Its exceptions propagate unchanged. Automatic mapping and decorated discovery are marked `RequiresUnreferencedCode`; this release does not claim validated trimming or Native AOT support.

## Existing link-based chains

`GenericChain<T>` remains the mutable object-link API. Replace `RepeatTimesOnFailure` / `repeatTimesOnFailure` with `MaxAttempts` / `maxAttempts`. The limit includes the first attempt, defaults to one, and rejects zero/negative values. Convert legacy zero to one; a positive legacy total-attempt limit keeps its value. Both stop and continue policies support retries; `StopOnFailure` applies after exhaustion.

Cancellation flags reset per run; payload mutations are retained. A link's cancellation request takes precedence over retrying or continuing. `OperationCanceledException` now propagates without retries or observers. Observer errors propagate outside retry handling. Null links/actions/observers now throw `ArgumentNullException` rather than failing later or disabling callbacks.

Discovery filters incompatible/non-constructible types, detects duplicate names before creating anything, and instantiates only the requested type. Each `AddDecoratedLink` call gets a new instance rather than reusing an eagerly cached object. Missing names now throw `KeyNotFoundException`; duplicates throw `InvalidOperationException`. Names must be nonblank. Loadable types remain available after partial type-load failures. Explicit `AddLink` registration avoids scanning.

## New reusable patterns

| Need | API | Completion behavior |
| --- | --- | --- |
| Ordered transformations with a fresh payload each run | `Pipeline<T>` / `AsyncPipeline<T>` | `Completed`, `Stopped`, `Cancelled`, or `Failed` result |
| First matching handler and optional fallback | `ChainOfResponsibility<TRequest,TResponse>` / async counterpart | `Handled`, `Unhandled`, `Cancelled`, or `Failed` result |
| Select a constructor/strategy by key | `Factory.For<TKey,TValue>()` | Immutable snapshot; constructs on each `Create` |
| Retry explicitly selected failures | `RetryPolicy` | Bounded attempts; no cancellation retries or implicit delay |

Pipeline and handler-chain registration returns a new immutable definition; retain the returned value. Independent runs receive their own input. Captured dependencies and mutable reference payloads remain the caller's concurrency responsibility. Unlike `GenericChain`, new pipelines always stop on an exhausted failure. Their result carries the latest returned payload; side effects are not rolled back.

New patterns return explicit cancellation/failure results; async collection methods and standalone retry operations propagate exceptions instead. Always inspect the pattern result's status. `HandlingResult.Value` throws unless handled. A handler failure never invokes the fallback. A pipeline retains a payload returned by a step before cancellation was observed, with or without a retry policy. Async pipeline observers return tasks and are awaited after the terminal outcome is selected, outside cancellation/retry checks; observer failures propagate. Completion observers run only for completion, and error observers run once after exhaustion.

See the [executable packaged examples](../examples/Version2Consumer/Program.cs), [README API guide](../readme.md#api-guide), and [measured costs](PERFORMANCE.md). CI checks the version 2 [CLR API baseline](PUBLIC-API.txt); changes to that baseline require a deliberate review, not automatic regeneration during validation.
