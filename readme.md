# FluentSharp for .Net Standard 2.0

FluentSharp is a simple set of extension methods that will helps you creating code in a fluent style way. Minimizing the amount of code lines written for common behaviors.

FluentSharp has the intention of remain small, not trying to solve every possible need or re invent already existing functionality and extensions. It will keep it simple!

* [Usings and imports](#using-and-imports)
* [Conditionals](#conditionals)
    * [```IfTrue()```](#iftrue-function)
    * [```IfFalse()```](#iffalse-function)
    * [```IfNull()```](#ifnull-function)
* [Exceptions](#exceptions)
    * [```Throw()```](#throw-function)
* [Iterators](#iterators)
    * [Synchronous iterators](#synchronous-iterators)
        * [```ForEvery()```](#forevery-function)
        * [```For()```](#for-function)
    * [Asynchronous iterators](#asynchronous-iterators)
        * [```ForEveryAsync()```](#foreveryasync-function)
        * [```ForAsync()```](#forasync-function)
* [Operations](#operations)
    * [```Then()```](#then-function)
    * [```ThenAsync()```](#thenasync-function)
* [Collection Alteration](#collection-alteration)
    * [```Alter()```](#alter-function)
    * [```AlterAsync()```](#alterasync-function)
* [Linq Extensions](#linq-extensions)
    * [```Intersect()```](#intersect-function)
    * [```Except()```](#except-function)
    * [Other extensions](#other-linq-extensions)
* [DataReader Extensions](#datareader-extensions)
    * [```ToList()```](#tolist-function)
    * [```ToMany()```](#tomany-function)
* [Pattern Implementations](#pattern-implementations)
    * [```GenericChain<T>()```](#generic-chain)

## Using and imports

In order to access the different extensions a set of namespaces will need to be added into the code.

#### For boolean expressions

```cs
using FunctionalSharp.Validators;
```

#### For collection iterators

```cs
using FunctionalSharp.Collections;
```

#### For Linq extensions

```cs
using FunctionalSharp.Linq;
```

#### For DataReader extensions

```cs
using FunctionalSharp.Data;
```

## Conditionals

It is common in coding to use conditions to derive the code execution. Most of these implementations are usually to validate input function parameters or executing one line after the condition is evaluated.

```cs
if (myVariable == 10) {
    DoSomething();
}
```

The previous is a common scenario in which, after a validation, a function (Or another line of code) is executed. Even though is easy to read, a fluent writing (And reading) could help describing our code better.

```cs
(myVariable == 10)
    .IfTrue()
    .Then(() => DoSomething());
```

### IfTrue function

Validates if the result of the boolean expression is true and returns a concrete [```Operations```](#operations) object.

The following operations will have effect only if ```IfTrue``` evaluates as true.

```cs
true.IfTrue()...;
```

From an expression

```cs
object obj = null;

(obj == null).IfTrue()...;
```

For any other boolean expression

```cs
(10 < 20).IfTrue()...;
```

### IfFalse function

Validates if the result of the boolean expression is false and returns a concrete [```Operations```](#operations) object.

The following operations will have effect only if ```IfFalse``` evaluates as false.

```cs
false.IfFalse()...;
```

From an expression

```cs
object obj = null;

(obj != null).IfFalse()...;
```

For any other boolean expression

```cs
(10 > 20).IfFalse()...;
```

### IfNull function

Validates if the object is null and returns a concrete [```Operations```](#operations) object.

The following operations will have effect only if ```IfNull``` evaluates as true.

```cs
null.IfNull()...;
```

From an expression

```cs
object obj = null;

obj.IfNull()...;
```

## Exceptions

Propagating malformed data across our code usually requires validations to be propagated too. In particular, input function parameters that are required by the subsequent code, can be reason enough to stop the code execution and fails with a controlled exception.

### Throw function

```Throw``` will throw the defined instance exception if ```IfNull```, ```IfFalse``` or ```IfTrue``` validates as expected.


```cs
[true|false|null].[IfFalse|IfTrue|IfNull].Throw(new Exception());
```

From an expression

```cs
public void ValidateUser(User user) 
{
    user.IfNull().Throw(new ArgumentNullException("User cannot be null"));
}
```

For any other boolean expression

```cs
(10 > 20).IfFalse().Throw(new Exception());
```

## Iterators

Iterators, as conditionals, are intended to work over collections in a fluent way. Also, can be used in combination with other collection interactors such as LinQ.

### Synchronous iterators

Iterates over collections in a synchronous way.

#### ForEvery function

Iterates across the collection passing the current element from the collection.

```cs
var collection = new List<int>() { 20, 21, 55, 77, 1 };
var total = 1;

collection.ForEvery(c => total *= c);
//total is equals to 1778700
```

#### For function

Iterate across the collection applying a particular condition. If the condition is not met (true), it stops.

```cs
collection.For(c => c < 22, c => Console.WriteLine(c));
```

The ```For``` function also can inject the current iteration index into the condition expression.

```cs
collection.For((c, index) => index < 3, c => Console.writeLine(c));
```

Also, ```For``` can receive a ```Func<T, bool>``` action to control the iteration flow. It will stop iterating the collection if the action returns false.

```cs
collection.For(c => c < 22);
```

```For``` function is a terminal function and will not return the original colelction.

### Asynchronous iterators

As its synchronous counterparts, asynchronous functions, ```ForEveryAsync``` and ```ForAsync``` are available.

#### ForEveryAsync function

Iterates, asynchonous, across the collection passing the current element from the collection.

```cs
var collection = new List<int>() { 20, 21, 55, 77, 1 };

await collection.ForEveryAsync(item => ...);
```
(For additional usage details see the synchonous versions)

#### ForAsync function

```cs
await collection.ForAsync(c => [boolean condition], c => ...);
```

```cs
await collection.ForAsync((c, index) => [boolean condition], c => ...);
```

```cs
await collection.ForAsync(c => [boolean condition]);
```

(For additional usage details see the synchonous versions)

## Operations

As result of a boolean evaluation a concrete object with additional operations will be returned.

### Throw function

Refers to [Throw function](#throw-function).

### Then function

Allow to continue processing a particular expression after a boolean evaluation. The ```Then``` function will execute an ```Action``` predicate.

```cs
var total = 0;

//...
//some operations
//...

(total < 10).IfTrue().Then(() => total++);
```

#### Then function for collections

Having a collection, passes the collection into a delegate for it manipulation.

```cs
var collection = new List<int>() { 20, 21, 55, 77, 1 };

collection.Then(c => DoSomethingWithCollection(c));
```
Because ```Then``` function is extending collections, it can be used together with LinQ.

```cs
collection.Where(c => c < 55).Then(c => DoSomething(c));
```

```Then``` function returns the input collection.

### ThenAsync function

```ThenAsync``` allows to perform the same operation than ```Then``` function but in an asynchonous context.

```cs
await collection.ThenAsync(c => ...);
```

## Collection Alteration

Handling collections with the existent extensions creates a new collection after it manipulation. The ```Then()``` function creates a context in which the current collection is passed into context but cannot be modified, or produce a new collection object after it manipulation. ```Alter()``` function, in the other hand, allows to produce a new collection from the executed context.

### Alter function

Having a collection, passes the collection into the expression context and returns a new collection based on the context result.

```cs
var collection = new List<int>() { 1, 2, 3, 4, 5 };

collection.Alter(c =>
{
    var list = c.ToList();
    list.Remove(1);
    return list;
}).Count();
```

### AlterAsync function

```AlterAsync``` allows to perform the same operation than ```Alter``` function but in an asynchonous context.

```cs
await collection.AlterAsync(c => ...);
```

## Linq Extensions

Linq is one of the APIs which allows us to code in a fluent way. Even though, there are some extensions that requires from us to write additional clases to handle equality. While this can give us great flexibility, force us to stack up our architecture.

Having the following set of data:

```cs
var A = new List<int> { 1, 2, 3, 4, 5, 6, 10, 11, 12, 13 };
var B = new List<int> { 6, 7, 8, 9, 10, 14, 15, 16, 17, 18 };
var D = new List<int> { 10, 11, 12, 13, 14, 15, 16, 17, 18 };
```

### Intersect function

Extends the current ```Intersect()``` adding the option of passing lambda expressions as comparer.

```cs
var C = A.Intersect(B, (a, b) => a == b);
```

### Except function

Extends the current ```Except()``` adding the option of passing lambda expressions as comparer.

```cs
var C = A.Except(D, (a, d) => a == d);
```

### Other Linq Extensions

Following a similar implementation, other LinQ extensions that requires ```IEqualityComparer<T>``` implementation are extended to support a lambda equality comparer function.

* ```Contains()```
* ```Distinct()```
* ```GroupBy()```
* ```GroupJoin()```
* ```Join()```
* ```ToDictionary()```
* ```ToLookup()```
* ```Union()```

## DataReader extensions

It is common when reading data from a database using a DataReader object, to iterate through the records with the intention of produce a collection of an internal known type. Translating a non-objectified type of data into one that our code can understand.

### ToList Function

The ```ToList<T>()``` extension uses the content of a single resulting query processed by an active DataReader, and convert the resulting rows into a list of expected type ```T``` based on the entity property type names and selected field names, and matching the query field types with the .Net property entity types.

Having the following set of data:

```text
Id    Name    Age
10    John    23
20    Peter   43
30    Claire  19
40    Julia   56
```

And the following SQL query:

```sql
SELECT Id, Name, Age FROM Users
```

With a type in our code:

```cs
class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
}
```

Can be converted to a ```List<User>()``` from the handling DataReader:

```cs
var listOfUsers = datareader.ToList<User>();
```

### ToMany Function

As ```ToList<T>()``` function, ```ToMany<...>()``` allows to read and translate a DataReader that has many query results. Each type used with ```ToMany<...>()``` function is handled as ordinal relevance. The returning type is a tuple with many values as types.

```cs
var (listOfUsers, listOfCompanies) = datareader.ToMany<User, Company>();
```

```ToMany<...>()``` provides up to 4 possible types, allowing to handle 4 possible query results from the DataReader. ```ToMany<T1, T2>()```, ```ToMany<T1, T2, T3>()```, ```ToMany<T1, T2, T3, T4>()```.

## Pattern Implementations

In order of speed up implementations, several design patterns are encapsulated usuing a pseudo functional approach.

### Generic Chain

```GenericChain<T>()``` encapsulates **chain of responsability** design pattern. It will execute a defined order of actions, in a sequential order, passing a user defined payload across the actions.

```cs
var chain = GenericChain<string>.Create(string.Empty);
```

if you need to use a custom type, the type must has an accessible contructor with zero parameters.

```cs
class MyCustomType { ... }

var chain = GenericChain<MyCustomType>.Create();
```

As any value-type, it is possible to inject a pre instantiated custom type and used as payload across the action calls.

```cs
var chain = GenericChain<MyCustomType>.Create(new MyCustomType());
```

Once the chain is created new links for the chain can be added.

```cs
chain
    .AddLink(event => Console.WriteLine("My action"))
    .AddLink(event => Console.WriteLine("My another action"));
```

An action can also be defined through a custom type derived from ```LinkBase``` class.

```cs
public class MyOwnAction : GenericChain<int>.LinkBase
{
    public override void OnExecute(GenericChain<int>.DataCargo data)
    {
        ...
    }
}
```

#### Configuring the chain

When a Generic Chain is instantiated it can be configured to behave differently when an exception is thrown.