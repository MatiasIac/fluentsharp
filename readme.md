# FluentSharp

[![CI](https://github.com/MatiasIac/fluentsharp/actions/workflows/ci.yml/badge.svg?branch=master)](https://github.com/MatiasIac/fluentsharp/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/FluentSharp.svg)](https://www.nuget.org/packages/FluentSharp/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/MatiasIac/fluentsharp/blob/master/LICENSE)

FluentSharp adds lightweight, fluent extensions to everyday C# code. Compose conditional actions, collection operations, LINQ comparisons, data reader mapping, and chains of responsibility using familiar .NET types and delegates.

The library aims to stay small and build on existing .NET functionality. It supports a functional style of composition, while allowing ordinary actions and mutable objects.

## Install

```shell
dotnet add package FluentSharp
```

The NuGet package is named **FluentSharp**. Its assembly and namespaces retain the original **FunctionalSharp** name for compatibility with existing code.

## Compatibility

The library targets **.NET Standard 2.0** and has no third-party runtime package dependencies. It can be consumed by compatible .NET implementations, including .NET 10 and .NET Framework 4.7.2 or later. See Microsoft's [.NET Standard compatibility guidance](https://learn.microsoft.com/en-us/dotnet/standard/net-standard).

Building this repository requires the **.NET 10 SDK** because the test project targets .NET 10. CI runs the tests on Windows and Linux; other compatible runtimes are not currently tested by CI.

## Quick start

```csharp
using System;
using System.Linq;
using FunctionalSharp.Collections;
using FunctionalSharp.Validators;

var values = new[] { 1, 2, 3, 4, 5 };

values
    .Where(value => value % 2 == 0)
    .Then(evens => Console.WriteLine($"Total: {evens.Sum()}"))
    .ForEvery(value => Console.WriteLine(value));

(values.Length > 0)
    .IfTrue()
    .Then(() => Console.WriteLine("Values are available."));
```

## API guide

- [Namespaces](#namespaces)
- [Conditions and exceptions](#conditions-and-exceptions)
- [Collections](#collections)
- [Async collection wrappers](#async-collection-wrappers)
- [LINQ equality comparisons](#linq-equality-comparisons)
- [Data readers](#data-readers)
- [Chains of responsibility](#chains-of-responsibility)
- [Build and test](#build-and-test)
- [Maintenance and releases](#maintenance-and-releases)

### Namespaces

| Namespace | Functionality |
| --- | --- |
| `FunctionalSharp.Validators` | `IfTrue`, `IfFalse`, and `IfNull` |
| `FunctionalSharp.Collections` | Collection actions, transformations, and iteration |
| `FunctionalSharp.Linq` | LINQ overloads accepting equality delegates |
| `FunctionalSharp.Data` | `DbDataReader` mapping |
| `FunctionalSharp.Patterns` | `GenericChain<T>`, configuration, and custom links |
| `FunctionalSharp.Decorators` | Named links using `[Link]` |

### Conditions and exceptions

`IfTrue()`, `IfFalse()`, and `IfNull()` return an operation object. When the condition matches, `Then()` executes an action and `Throw()` throws the supplied exception. When it does not match, subsequent operations do nothing.

```csharp
using System;
using FunctionalSharp.Validators;

var count = 0;
(count < 10).IfTrue().Then(() => count++);

var isValid = false;
isValid.IfFalse().Throw(new InvalidOperationException("Invalid state."));
```

For argument validation:

```csharp
user.IfNull().Throw(new ArgumentNullException(nameof(user)));
```

`Then()` returns the same operation object, so multiple actions can be chained. The original condition is evaluated once. `Throw()` is terminal.

### Collections

`Then()` passes the entire sequence to an action and returns the original sequence. `Alter()` returns the sequence produced by your delegate, which may be a new collection or the original one.

```csharp
using System;
using System.Linq;
using FunctionalSharp.Collections;

var values = new[] { 1, 2, 3, 4, 5 };

var result = values
    .Where(value => value < 4)
    .Then(items => Console.WriteLine(items.Count()))
    .Alter(items => items.Concat(new[] { 10 }))
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

`For()` and `ForEvery()` return `void`. These extensions do not automatically materialize a sequence; enumerating it inside `Then()` and again afterward can execute a deferred query twice.

### Async collection wrappers

The available wrappers are `ThenAsync()`, `AlterAsync()`, `ForEveryAsync()`, and the three `ForAsync()` overloads.

```csharp
await values.ForEveryAsync(value => Console.WriteLine(value));
await values.ForAsync(value => value < 22, value => Console.WriteLine(value));
await values.ForAsync((value, index) => index < 3, value => Console.WriteLine(value));
await values.ForAsync(value => value < 22);

var original = await values.ThenAsync(items => Console.WriteLine(items.Count()));
var transformed = await values.AlterAsync(items => items.Where(value => value > 20));
```

These methods accept **synchronous delegates**. `ForEveryAsync()` and `ForAsync()` run synchronous iteration through `Task.Run`. `ThenAsync()` and `AlterAsync()` execute their delegate synchronously and wrap the result in a task. They do not support awaiting asynchronous callbacks; passing an `async` lambda to an `Action` parameter creates `async void` work that the wrapper cannot await.

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

The delegate must describe equality. Hash-based LINQ operations also require equal values to have equal hash codes. Overloads without a hash delegate use the object's existing `GetHashCode()` implementation. When comparing objects by a property, the `Distinct` overload with a hash delegate lets you supply both:

```csharp
var uniqueUsers = users.Distinct(
    (left, right) => left.Id == right.Id,
    user => user.Id.GetHashCode());
```

That overload currently requires a type with a public parameterless constructor. For other hash-based operations, use types whose `GetHashCode()` agrees with the equality delegate, or an appropriate standard LINQ comparer.

### Data readers

`ToList<T>()` maps rows from the current `DbDataReader` result set to public properties with matching column names. The destination type needs a public parameterless constructor.

```csharp
using FunctionalSharp.Data;

// For a reader containing columns Id, Name, and Age:
var users = reader.ToList<User>();

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
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

Missing result sets produce empty lists. The caller owns and disposes the reader. Mapping uses reflection and `Convert.ChangeType`; columns must match writable properties and contain convertible values. There is no special handling for `DBNull`, nullable property types, or unmatched columns.

### Chains of responsibility

`GenericChain<T>` executes links in order, passing a shared `DataCargo<T>` containing the payload and a cancellation flag.

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

Set `data.Cancel = true` in a link to stop processing. Cancellation also suppresses `OnCompleted()`. By default, an exception stops the chain and calls `OnError()` if a handler is registered; caught link exceptions are not automatically rethrown.

```csharp
var chain = GenericChain<int>.Create(0,
    new Configuration(stopOnFailure: false, repeatTimesOnFailure: 3));
```

In the current implementation, this configuration allows up to **three total attempts** per failing link, then continues. Retries run only when `stopOnFailure` is false, and `OnError()` runs for each failed attempt. A zero repeat count currently stops on a failure even when `stopOnFailure` is false.

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

To discover a link by name, decorate a concrete link class with `[Link("Increment")]` from `FunctionalSharp.Decorators`, then call `.AddDecoratedLink("Increment")`. Discovery scans loaded assemblies; use unique names and constructible link types matching the chain's payload type.

## Build and test

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), clone the repository, and run these commands from its root:

```shell
dotnet restore FunctionalSharp/FunctionalSharp.sln
dotnet build FunctionalSharp/FunctionalSharp.sln --configuration Release --no-restore
dotnet test FunctionalSharpTests/FunctionalSharpTests.csproj --configuration Release --no-build --no-restore --logger "trx;LogFileName=tests.trx" --collect:"XPlat Code Coverage" --results-directory artifacts/test-results
dotnet pack FunctionalSharp/FunctionalSharp.csproj --configuration Release --no-build --no-restore --output artifacts/packages
```

Test results and Cobertura coverage reports are written under `artifacts/test-results`. The `.nupkg` and `.snupkg` files are written under `artifacts/packages`. Package creation is an explicit step; building alone does not create a package.

Pushes to `master` run the same checks on Windows and Linux through GitHub Actions. Results and packages are available as workflow artifacts.

## Maintenance and releases

FluentSharp is maintained solely by Matías Iacono. Development takes place directly on `master`; external pull requests are not accepted.

See the [changelog](https://github.com/MatiasIac/fluentsharp/blob/master/CHANGELOG.md) for changes and the [release guide](https://github.com/MatiasIac/fluentsharp/blob/master/docs/RELEASING.md) for the maintainer's NuGet setup and release process. Publishing a versioned GitHub Release starts the verified NuGet publishing workflow.

## License

FluentSharp is licensed under the [MIT license](https://github.com/MatiasIac/fluentsharp/blob/master/LICENSE).
