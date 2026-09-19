# FluentSharp

[![CI](https://github.com/MatiasIac/fluentsharp/actions/workflows/ci.yml/badge.svg?branch=master)](https://github.com/MatiasIac/fluentsharp/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/FluentSharp.svg)](https://www.nuget.org/packages/FluentSharp/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/MatiasIac/fluentsharp/blob/master/LICENSE)

FluentSharp adds lightweight, fluent extensions to everyday C# code. Compose conditional actions, collection operations, LINQ comparisons, data reader mapping, and chains of responsibility using familiar .NET types and delegates.

The library aims to stay small and build on existing .NET functionality. It supports a functional style of composition, while allowing ordinary actions and mutable objects.

This checkout is prepared for the stable **`2.0.0`** release. **1.0.7 is the final legacy release**. Version 2 intentionally changes APIs and requires .NET 10 / C# 14. See the [migration guide](https://github.com/MatiasIac/fluentsharp/blob/master/docs/MIGRATING-TO-V2.md), [implementation status](https://github.com/MatiasIac/fluentsharp/blob/master/docs/DEVELOPMENT-PLAN.md), and [complete executable examples](https://github.com/MatiasIac/fluentsharp/blob/master/examples/Version2Consumer/Program.cs).

## Install

```shell
dotnet add package FluentSharp --version 2.0.0
```

Use this version from your local package feed until it is published to NuGet. The [build instructions](#build-and-test) create the package and run a consumer against it. The package is named **FluentSharp**; its assembly and namespaces retain **FunctionalSharp**.

## Compatibility

The library targets **.NET 10**, uses **C# 14 extension properties**, and has no third-party runtime package dependencies. Consumers need a compiler supporting C# 14 to use the property syntax. Projects requiring .NET Standard 2.0 or .NET Framework should remain on `FluentSharp` 1.0.7.

Build with the **.NET 10 SDK**. CI runs tests, verifies the public API baseline, and compiles and runs the packed NuGet consumer on Windows and Linux. Trimming and Native AOT support are not claimed; automatic property mapping and decorated-link discovery use reflection. Explicit row mappers and delegate-based patterns avoid those reflection paths.

## Quick start

```csharp
using System;
using System.Linq;
using FunctionalSharp.Collections;
using FunctionalSharp.Composition;
using FunctionalSharp.Validators;

var values = new[] { 1, 2, 3, 4, 5 };

values
    .Where(value => value % 2 == 0)
    .Tap(evens => Console.WriteLine($"Total: {evens.Sum()}"))
    .ForEvery(value => Console.WriteLine(value));

(values.Length > 0)
    .IfTrue
    .Then(() => Console.WriteLine("Values are available."));
```

## API guide

- [Namespaces](#namespaces)
- [Conditions and exceptions](#conditions-and-exceptions)
- [Value composition](#value-composition)
- [Collections](#collections)
- [Async collection operations](#async-collection-operations)
- [LINQ equality comparisons](#linq-equality-comparisons)
- [Data readers](#data-readers)
- [Reusable pipelines](#reusable-pipelines)
- [Chains of responsibility](#chains-of-responsibility)
- [Typed factories and retries](#typed-factories-and-retries)
- [Link-based processing](#link-based-processing)
- [Build and test](#build-and-test)
- [Maintenance and releases](#maintenance-and-releases)

### Namespaces

| Namespace | Functionality |
| --- | --- |
| `FunctionalSharp.Validators` | `IfTrue`, `IfFalse`, `IfNull`, and typed `IfNotNull` properties |
| `FunctionalSharp.Composition` | Immediate `Tap` and `Pipe` value composition |
| `FunctionalSharp.Operations` | Immutable `Condition` and `ValueCondition<T>` stages |
| `FunctionalSharp.Collections` | Collection actions, transformations, and iteration |
| `FunctionalSharp.Linq` | LINQ overloads accepting equality delegates |
| `FunctionalSharp.Data` | `DbDataReader` mapping |
| `FunctionalSharp.Patterns` | Pipelines, handler chains, typed factories, retry policies, and object links |
| `FunctionalSharp.Decorators` | Named links using `[Link]` |

### Conditions and exceptions

`IfTrue`, `IfFalse`, and `IfNull` capture a condition in an immutable `Condition` struct. `Then` executes an action when matched. `Throw` calls an exception factory only when matched, preserving compile-time constructor checking and lazy argument evaluation.

```csharp
using System;
using FunctionalSharp.Validators;

var count = 0;
(count < 10).IfTrue.Then(() => count++);

var isValid = false;
isValid.IfFalse.Throw<InvalidOperationException>(() => new("Invalid state."));
```

For argument validation:

```csharp
user.IfNull.Throw(() => new ArgumentNullException(nameof(user)));
```

`Then` returns the same captured condition. `Throw<TException>()` also supports a public parameterless constructor, invoked only on a match. `Throw` is terminal. Default `Condition` and `ValueCondition<T>` values are inactive. Required delegates are validated even when inactive; a matched exception factory returning null reports `InvalidOperationException`.

`IfNotNull` keeps the underlying type for callbacks, for both references and nullable value types. `Match` evaluates exactly one result branch:

```csharp
string? name = null;
name.IfNotNull.Then(text => Console.WriteLine(text.Length));
var label = name.IfNotNull.Match(text => text.ToUpperInvariant(), () => "Anonymous");
var access = isValid.IfTrue.Match(() => "Allowed", () => "Denied");
int? count = 3;
count.IfNotNull.Then(number => Console.WriteLine(number + 1)); // number is int
```

The value is captured when you read the property. A chained null check does not change the compiler's null-state analysis of the original variable; use the typed callback when you need a non-null value. Synchronous `Then`/`Tap` actions should return `void`; use task-returning APIs for asynchronous work.

### Value composition

`Tap` executes an action immediately and returns the original value with its static type preserved. `Pipe` transforms a value into any result type. Both invoke their delegate once, allow null values to flow through, and propagate delegate exceptions. They do not enumerate collections themselves.

```csharp
using FunctionalSharp.Composition;

var receipt = order.Tap(ValidateOrder).Pipe(CalculateTotal).Pipe(CreateReceipt);
```

These replace the collection-only synchronous `Then` and `Alter` methods from version 1.

### Collections

Use `Tap` to observe a whole sequence and `Pipe` to transform it, then apply collection iteration when required.

```csharp
using System;
using System.Linq;
using FunctionalSharp.Collections;
using FunctionalSharp.Composition;

var values = new[] { 1, 2, 3, 4, 5 };

var result = values
    .Where(value => value < 4)
    .Tap(items => Console.WriteLine(items.Count()))
    .Pipe(items => items.Concat(new[] { 10 }))
    .ToList();
// result: 1, 2, 3, 10
```

`ForEvery()` executes an action for each item. `For()` stops at the first false condition; it does not resume at later matching items.

```csharp
var values = new[] { 20, 21, 55, 77, 1 };

values.ForEvery(value => Console.WriteLine(value));
values.For(value => value < 22, value => Console.WriteLine(value)); // 20, 21
values.For((value, index) => index < 3, value => Console.WriteLine(value));

values.For(value =>
{
    Console.WriteLine(value);
    return value != 55; // Stop after processing 55.
});
```

`For` and `ForEvery` return `void`, reject null arguments immediately, and dispose their iterator on all exits. These extensions do not automatically materialize a sequence; enumerating it inside `Tap` and again afterward can execute a deferred query twice.

### Async collection operations

`ThenAsync()`, `AlterAsync()`, `ForEveryAsync()`, and `ForAsync()` accept **task-returning delegates** and await their completion. Every form has a token-aware callback overload and an optional `CancellationToken` argument. Both `async` lambdas and methods returning tasks are supported.

```csharp
using FunctionalSharp.Collections;

// SaveAsync is your Task-returning operation. Items are processed sequentially.
await orders.ForEveryAsync(order => SaveAsync(order));
await orders.ForEveryAsync((order, token) => SaveAsync(order, token), cancellationToken);

// Stop before the first order that is not ready, or after at most three orders.
await orders.ForAsync(order => order.IsReady,
    (order, token) => SaveAsync(order, token), cancellationToken);
await orders.ForAsync((order, index) => index < 3,
    (order, token) => SaveAsync(order, token), cancellationToken);

// A Task<bool> callback can combine asynchronous work with a continuation decision.
await orders.ForAsync(async (order, token) =>
{
    await SaveAsync(order, token);
    return !order.IsLast; // Stop after saving this order when false.
}, cancellationToken);

// AuditAsync returns Task; EnrichAsync returns Task<IEnumerable<Order>>.
var original = await orders.ThenAsync((items, token) => AuditAsync(items, token), cancellationToken);
var transformed = await orders.AlterAsync((items, token) => EnrichAsync(items, token), cancellationToken);
```

Iteration preserves enumeration order and awaits each callback before reading the next item. `ForAsync` conditions are synchronous, run before the action, and stop at the first false result; indexed conditions start at zero. The `Task<bool>` form stops after awaiting a callback whose result is false. Iterators are disposed on completion, early stopping, failure, and cancellation. Enumeration itself is synchronous; these methods do not schedule work through `Task.Run` or promise a particular execution thread.

`ThenAsync` returns the original sequence; `AlterAsync` returns the sequence produced by its callback. Neither method enumerates the input or output itself. Subsequent enumeration can execute a deferred query again.

Cancellation is cooperative: the supplied token is checked before enumeration/callbacks, between items, and after callbacks, and is passed to token-aware delegates. A running callback is always awaited. If it ignores cancellation, the operation observes cancellation once that callback finishes. Callback, condition, and enumeration exceptions propagate through the returned task and stop further processing. Null sequences, actions, and required conditions are rejected synchronously with `ArgumentNullException`; a callback returning a null task or a transformation producing a null sequence faults the task with `InvalidOperationException`.

**Migrating from 1.0.7:** the synchronous delegate overloads on these async methods have been removed. Use `Tap`, `Pipe`, `ForEvery`, and `For` for synchronous work. For asynchronous work, return a task or use an `async` lambda, then await the operation. For example, `await orders.ForEveryAsync(async order => await SaveAsync(order))` now waits for every save and propagates its errors.

### LINQ equality comparisons

Use equality delegates with `Intersect`, `Except`, `Contains`, `Distinct`, `GroupBy`, `GroupJoin`, `Join`, `ToDictionary`, `ToLookup`, and `Union`.

```csharp
using FunctionalSharp.Linq;

var first = new[] { 1, 2, 3, 4 };
var second = new[] { 3, 4, 5 };

var shared = first.Intersect(second, (left, right) => left == right); // 3, 4
var remaining = first.Except(second, (left, right) => left == right); // 1, 2
var containsThree = first.Contains(3, (left, right) => left == right);
```

The delegate must describe equality and handle null values if they are present. Equality-only overloads use a constant hash so values considered equal are compared even when their existing hash codes differ. For example:

```csharp
var uniqueNames = new[] { "Alice", "ALICE", "Bob" }
    .Distinct(StringComparer.OrdinalIgnoreCase.Equals); // Alice, Bob
```

The constant-hash fallback can require **quadratic work** for large collections. All equality-delegate methods accept an optional final matching hash delegate:

```csharp
var uniqueUsers = users.Distinct(
    (left, right) => left?.Id == right?.Id,
    user => user.Id.GetHashCode());
```

Equal values must receive equal hash codes from the supplied hash delegate. No constructor constraint is required. A null hash delegate uses the constant-hash fallback. `Contains` performs linear comparisons and does not use hashing. The operations retain standard LINQ evaluation timing and null-key rules.

For reuse across LINQ, sets, and dictionaries, create a key comparer:

```csharp
var byId = KeyComparer.By((User user) => user.Id);
var uniqueUsers = users.Distinct(byId);
var usersByKey = new HashSet<User>(byId);
```

`KeyComparer.By` optionally accepts a comparer for the selected key. Null source values only equal other null sources and never invoke the selector. Null keys hash to zero. Keep selected keys stable while their values are stored in a hash collection. See [measurements](https://github.com/MatiasIac/fluentsharp/blob/master/docs/PERFORMANCE.md) for the cost of the constant-hash fallback.

### Data readers

`ToList<T>()` maps rows from the current `DbDataReader` result set to public writable instance properties with matching column names. Classes need a public parameterless constructor; structs are also supported. Properties without a matching column retain their initial values.

```csharp
using FunctionalSharp.Data;

// For a reader containing columns Id, Name, and Age:
var users = reader.ToList<User>();

public class User
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int? Age { get; set; } // A database null maps to null.
}
```

Pass `ignoreCase: true` to match column and property names without case sensitivity:

```csharp
var users = reader.ToList<User>(ignoreCase: true);
```

`ToMany` maps two, three, or four consecutive result sets into a tuple of lists:

```csharp
var (users, companies) = reader.ToMany<User, Company>();
```

Missing result sets produce empty lists. The caller owns and disposes the reader, including after a mapping error. `ToList` consumes only the current result's remaining rows; `ToMany` advances through the requested results. Property mappings are resolved once for each result set, so different column orders and schemas are handled independently.

| Database value | Mapping rule |
| --- | --- |
| `DBNull` | Becomes null for reference or nullable value properties. Non-nullable value properties reject it. Reference-type nullable annotations are not inspected. |
| Already assignable value | Assigned directly, including GUIDs, byte arrays, and date/time values of the destination type. |
| Nullable value property | Non-null values convert to the underlying type, such as `int` for `int?`. |
| GUID string | Parsed as a `Guid`, including for `Guid?`. |
| Enum name or number | Names are case-sensitive; numeric values convert to the enum's underlying type. Undefined numeric values are allowed, as with an ordinary enum cast. |
| Other convertible value | Uses `Convert.ChangeType` with invariant culture. For example, decimal text uses `.` as its decimal separator. |

Every column must match exactly one eligible property. Unknown columns, read-only/private-setter/static/indexed properties, ambiguous case-insensitive matches, and columns targeting the same property are rejected with `DataException` before reading rows. Empty result sets also validate their schemas. `ignoreCase` affects property-name matching only.

Conversion and setter errors include the column name/ordinal, destination property/type, and one-based row number within the current mapping call. The underlying exception is retained in `InnerException`. Reader and row-constructor failures propagate to the caller.

**Migrating from 1.0.7:** database nulls now map to actual nulls, including for strings. Mapping is limited to public instance setters, schema validation also applies to empty results, and standard conversion errors are reported through `DataException`. Text conversions use invariant culture rather than the current culture.

For immutable objects, custom constructors, or explicit conversion rules, supply a row mapper:

```csharp
var receipts = reader.ToList(row => new Receipt(row.GetInt32(0), row.GetDecimal(1)));
```

This overload uses no reflection or `new()` constraint. The mapper receives each current row once and must not advance or dispose the reader. Mapper exceptions propagate unchanged, and the reader remains caller-owned.

### Reusable pipelines

`Pipeline<T>` is an immutable sequence of transformations. Each `Run` takes a fresh payload. `Then`, `StopWhen`, and observer registration return new definitions; keep the returned definition when extending it.

```csharp
using FunctionalSharp.Patterns;

var pipeline = Pipeline<decimal>.Create()
    .Then(total => total * 1.15m)
    .StopWhen(total => total > 1000)
    .Then(total => decimal.Round(total, 2));

var result = pipeline.Run(20m);
// result.Status == ExecutionStatus.Completed; result.Payload == 23m
```

The result distinguishes `Completed`, `Stopped`, `Cancelled`, and `Failed`, with the most recently returned payload, the zero-based stopping/failing step, and the original error where applicable. `StopWhen` is itself a step. Step failures stop the pipeline after any retry policy is exhausted. `OnCompleted` runs only on completion; `OnError` observes the final failure once. Observer exceptions propagate, outside retry handling.

`AsyncPipeline<T>` awaits each transformation and its observers. It has `RunAsync` and task-returning `Then`, `OnCompleted`, and `OnError` delegates:

```csharp
var pipeline = AsyncPipeline<Order>.Create()
    .Then((order, token) => EnrichOrderAsync(order, token))
    .Then((order, token) => SaveOrderAsync(order, token)); // Task<Order>
var result = await pipeline.RunAsync(order, cancellationToken);
```

Both versions accept cancellation tokens. Step cancellation and `OperationCanceledException` produce a `Cancelled` result, without error/completion observers. Running async work is always awaited; cancellation is cooperative. If a step returns a new payload before cancellation is observed, the result retains that payload, including when a retry policy wraps the step. Observers report an already selected outcome and run outside retry/cancellation checks; their failures propagate. Definitions can be reused concurrently with independent payloads and thread-safe handlers. Reference mutations and external side effects are never rolled back.

### Chains of responsibility

`ChainOfResponsibility<TRequest, TResponse>` runs the first matching handler and skips all later predicates and handlers. `Otherwise` is a fallback only when nothing matches:

```csharp
var approvals = ChainOfResponsibility<decimal, string>.Create()
    .When(total => total >= 1000, total => "Manual review")
    .When(total => total > 0, total => "Approved")
    .Otherwise(total => "Empty order");

var result = approvals.Handle(20m);
// result.Status == HandlingStatus.Handled; result.Value == "Approved"
```

Without a fallback, unmatched requests return `Unhandled`. Predicate/handler failures return `Failed`; cancellation returns `Cancelled`. Failure and cancellation never fall through to another handler. `Value` is available only for `Handled` results; it throws otherwise. `Error` retains the original exception. `HandlerIndex` is zero-based; the fallback's index equals the number of registered handlers.

`AsyncChainOfResponsibility<TRequest, TResponse>` offers `HandleAsync`, synchronous or task-returning predicates, and awaited handlers/fallbacks. Token-aware delegates receive the supplied token. Definitions are immutable and reusable; handlers own the thread safety of captured dependencies.

### Typed factories and retries

`Factory.For<TKey, TValue>()` builds a registry of constructor delegates. It is useful for selecting an implementation by a known key without a container:

```csharp
var factory = Factory.For<string, INotifier>(StringComparer.OrdinalIgnoreCase)
    .Register("email", () => new EmailNotifier())
    .Register("sms", () => new SmsNotifier())
    .Build();
var notifier = factory.Create("EMAIL");
```

Registration rejects duplicate keys; `Build` creates an independent immutable snapshot without constructing values. Each successful `Create` invokes its constructor again. Unknown keys throw `KeyNotFoundException`, or return false through `TryCreate`. Constructor errors propagate. The caller owns object disposal, caching, and lifetimes. This is a typed registry/strategy selector, not an inheritance-based Factory Method framework.

`RetryPolicy` decorates an operation or an individual pipeline step:

```csharp
var retry = new RetryPolicy(maxAttempts: 3, shouldRetry: error => error is IOException);
var response = await retry.ExecuteAsync(token => SendAsync(token), cancellationToken);
var pipeline = AsyncPipeline<Order>.Create().Then((order, token) => SaveOrderAsync(order, token), retry);
```

The attempt limit includes the first call. Only failures selected by the predicate are retried, with no implicit delay. Cancellation is never retried, and a cancelled token prevents another attempt. Exhaustion rethrows the original error for standalone operations; pipelines return a failed result. Predicate errors propagate. Use this only where repeating the operation's effects is acceptable.

### Link-based processing

`GenericChain<T>` retains the object-link model: it executes links in order, passing a shared `DataCargo<T>` containing the payload and a cancellation flag. It is a mutable processing chain, with different lifecycle and failure behavior from the immutable pipelines and first-match handler chains above.

```csharp
using System;
using FunctionalSharp.Patterns;

GenericChain<int>.Create(0)
    .AddLink(data => data.Payload += 10)
    .AddLink(data => data.Payload *= 2)
    .OnError((payload, exception) => Console.WriteLine(exception.Message))
    .OnCompleted(payload => Console.WriteLine(payload)) // 20
    .Run();
```

`Create()` uses a default value-type payload or constructs a reference type with an accessible parameterless constructor. Pass an existing payload to `Create(payload)` to retain that instance. For strings, supply a value such as `string.Empty`.

Set `data.Cancel = true` in a link to stop processing and suppress `OnCompleted()`. If the link also throws an ordinary exception, `OnError()` is called once before stopping; the link is not retried. An `OperationCanceledException` propagates immediately to the caller without retries, `OnError()`, or `OnCompleted()`.

Configure ordinary failures using an attempt limit and a stop/continue policy:

```csharp
var chain = GenericChain<int>.Create(0,
    new Configuration(stopOnFailure: false, maxAttempts: 3));
```

`MaxAttempts` includes the initial attempt and must be at least one. Each link gets its own attempt budget on every run. A successful attempt immediately advances to the next link. Retries run immediately, and payload changes from failed attempts are retained.

| Configuration | Behavior when the link keeps failing |
| --- | --- |
| `new Configuration()` | One attempt, then stop. |
| `new Configuration(stopOnFailure: false)` | One attempt, then continue to the next link. |
| `new Configuration(stopOnFailure: true, maxAttempts: 3)` | Up to three attempts, then stop. |
| `new Configuration(stopOnFailure: false, maxAttempts: 3)` | Up to three attempts, then continue. |

`OnError()` runs for every ordinary failed attempt, including failures followed by a successful retry. Caught link exceptions are not automatically rethrown. `OnCompleted()` runs once when execution reaches the end, including after failures configured to continue. Exceptions from either callback propagate immediately and are not retried or sent back to `OnError()`.

Calling `Run()` again resets cancellation and attempt budgets, while retaining the current payload. It does not undo changes from earlier runs or failed attempts.

**Migrating from 1.0.7:** replace the `repeatTimesOnFailure` argument with `maxAttempts`, and the `RepeatTimesOnFailure` property with `MaxAttempts`. A legacy zero becomes `maxAttempts: 1`; a positive total-attempt limit keeps its value. Unlike version 1, `stopOnFailure: true` permits retries when `maxAttempts` is greater than one; use one for immediate stopping. Zero and negative attempt limits are rejected. Cancellation exceptions now propagate instead of being handled as ordinary failures.

Custom links can inherit from `LinkBase<T>`:

```csharp
public class Increment : LinkBase<int>
{
    public override void OnExecute(DataCargo<int> data)
    {
        data.Payload++;
    }
}

// Add the link with chain.AddLink(new Increment()).
```

To discover a link by name, decorate a concrete link class with `[Link("Increment")]` from `FunctionalSharp.Decorators`, then call `.AddDecoratedLink("Increment")`. Discovery scans loaded assemblies and selects only subclasses of `LinkBase<T>` for the chain's payload type. It excludes abstract classes, types with unbound generic parameters, and classes without a public parameterless constructor before creating instances. A concrete link can inherit its decoration from a base class.

Names must be unique among eligible links for the same payload type; links for different payload types can share a name. Duplicate names are diagnosed before any link is constructed. Only the requested type is instantiated, freshly for each `AddDecoratedLink` call. Excluded or unknown names throw `KeyNotFoundException`. Discovery retains loadable types after a partial type-load failure and is marked as requiring unreferenced code. Prefer `AddLink(instance)` or `AddLink(delegate)` for explicit registration.

## Build and test

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), clone the repository, and run these commands from its root:

```shell
dotnet restore FunctionalSharp/FunctionalSharp.sln
dotnet build FunctionalSharp/FunctionalSharp.sln --configuration Release --no-restore
dotnet test FunctionalSharpTests/FunctionalSharpTests.csproj --configuration Release --no-build --no-restore --logger "trx;LogFileName=tests.trx" --collect:"XPlat Code Coverage" --results-directory artifacts/test-results
dotnet pack FunctionalSharp/FunctionalSharp.csproj --configuration Release --no-build --no-restore --output artifacts/packages
dotnet run --project tools/ApiSnapshot/ApiSnapshot.csproj --configuration Release -- --verify docs/PUBLIC-API.txt
dotnet restore examples/Version2Consumer/Version2Consumer.csproj
dotnet run --project examples/Version2Consumer/Version2Consumer.csproj --configuration Release --no-restore
```

Test results and Cobertura coverage reports are written under `artifacts/test-results`. The `.nupkg` and `.snupkg` files are written under `artifacts/packages`. Package creation is an explicit step; building alone does not create a package.

The example project restores only from `artifacts/packages`, uses an isolated package cache, and treats nullable/compiler warnings as errors. When testing another version, pass `-p:FluentSharpVersion=<version>` to both its restore and run commands. Pushes to `master` run these checks on Windows and Linux. Results and packages are available as workflow artifacts. [Benchmark instructions and results](https://github.com/MatiasIac/fluentsharp/blob/master/docs/PERFORMANCE.md) are separate from CI timing checks.

NuGet caches by version. When repacking the same unpublished development version locally, refresh its generated folder under `artifacts/consumer-cache/fluentsharp` before restoring the example. Unit tests use a project reference and always test the current source.

## Maintenance and releases

FluentSharp is maintained solely by Matías Iacono. Development takes place directly on `master`; external pull requests are not accepted.

See the [changelog](https://github.com/MatiasIac/fluentsharp/blob/master/CHANGELOG.md) for changes and the [release guide](https://github.com/MatiasIac/fluentsharp/blob/master/docs/RELEASING.md) for the maintainer's NuGet setup and release process. Publishing a versioned GitHub Release starts the verified NuGet publishing workflow.

## License

FluentSharp is licensed under the [MIT license](https://github.com/MatiasIac/fluentsharp/blob/master/LICENSE).
