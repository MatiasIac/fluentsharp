using System.Data;
using FunctionalSharp.Collections;
using FunctionalSharp.Composition;
using FunctionalSharp.Data;
using FunctionalSharp.Linq;
using FunctionalSharp.Patterns;
using FunctionalSharp.Validators;

// This executable references the packed NuGet package, not the source project.
var order = new Order(7, 20m, "email");
var validated = 0;
var receipt = order.Tap(Validate).Pipe(CreateReceipt);
Check(validated == 1 && receipt.Total == 20m, "value composition");

var messageCalls = 0;
false.IfTrue.Throw<OrderException>(() => new(order.Id, BuildMessage()));
Check(messageCalls == 0, "lazy exception arguments");
try { true.IfTrue.Throw<OrderException>(() => new(order.Id, BuildMessage())); }
catch (OrderException exception) { Check(exception.OrderId == 7 && messageCalls == 1, "custom constructor"); }

string? missing = null;
Check(missing.IfNull.IsMatched, "null condition");
Check(missing.IfNotNull.Match(text => text.Length, () => 0) == 0, "typed fallback");
string? name = "Alice";
name.IfNotNull.Then(text => Check(text.Length == 5, "non-null callback annotation"));
int? count = 4;
count.IfNotNull.Then(number => Check(number + 1 == 5, "nullable value callback"));
Check(true.IfTrue.Match(() => 1, () => throw new Exception()) == 1, "lazy branch");
try { false.IfTrue.Then(null!); throw new Exception("Expected a null argument error."); }
catch (ArgumentNullException) { }

var saved = new List<int>();
Order[] orders = [order, order with { Id = 8 }];
await orders.ForEveryAsync(SaveAsync);
await orders.ForAsync((item, index) => index < 1, SaveAsync);
Check(saved.SequenceEqual(new[] { 7, 8, 7 }), "awaited iteration and method groups");
var observed = await orders.ThenAsync(items => Task.CompletedTask);
var transformed = await orders.AlterAsync(items => Task.FromResult(items.Where(item => item.Id == 7)));
Check(ReferenceEquals(orders, observed) && transformed.Count() == 1, "async sequences");

var pipeline = Pipeline<Order>.Create().Then(item => item with { Total = item.Total + 2 })
    .StopWhen(item => item.Total > 100);
Check(pipeline.Run(order).Payload.Total == 22m, "pipeline transformation");
Check(pipeline.Run(order with { Total = 120 }).Status == ExecutionStatus.Stopped, "pipeline stop");
Check(pipeline.Run(order).Payload.Total == 22m, "independent pipeline runs");
var asyncPipeline = AsyncPipeline<Order>.Create().Then(async (item, token) =>
{
    await SaveAsync(item, token);
    return item;
});
Check((await asyncPipeline.RunAsync(order)).Status == ExecutionStatus.Completed, "async pipeline");

var routing = ChainOfResponsibility<Order, string>.Create()
    .When(item => item.Total >= 100, item => "manual review")
    .When(item => item.Total > 0, item => "approved")
    .Otherwise(item => "empty");
Check(routing.Handle(order).Value == "approved", "first-match routing");
var asyncRouting = AsyncChainOfResponsibility<Order, string>.Create()
    .When(item => item.Total > 0, (item, token) => Task.FromResult("approved"));
Check((await asyncRouting.HandleAsync(order)).Value == "approved", "async routing");

var factory = Factory.For<string, INotifier>(StringComparer.OrdinalIgnoreCase)
    .Register("email", () => new EmailNotifier()).Register("sms", () => new SmsNotifier()).Build();
Check(factory.Create(order.Channel).Describe(receipt) == "Email receipt 7", "factory selection");
Check(!factory.TryCreate("unknown", out _), "unknown factory key");

var attempts = 0;
var retry = new RetryPolicy(3, error => error is IOException);
var delivered = await retry.ExecuteAsync(token =>
    ++attempts < 3 ? Task.FromException<string>(new IOException()) : Task.FromResult("delivered"));
Check(delivered == "delivered" && attempts == 3, "retry attempt limit");

var sameOrder = order with { Total = 99 };
var comparer = KeyComparer.By((Order item) => item.Id);
Check(new HashSet<Order>([order, sameOrder], comparer).Count == 1, "key comparer");
Check(new[] { "a", "A" }.Distinct(StringComparer.OrdinalIgnoreCase.Equals, StringComparer.OrdinalIgnoreCase.GetHashCode).Count() == 1, "hash delegate");

using var table = new DataTable();
table.Columns.Add("id", typeof(int));
table.Columns.Add("total", typeof(decimal));
table.Rows.Add(7, 20m);
using var reader = table.CreateDataReader();
var mapped = reader.ToList(row => new Receipt(row.GetInt32(0), row.GetDecimal(1)));
Check(mapped.Single() == receipt && !reader.IsClosed, "explicit row mapping and ownership");
Console.WriteLine("All packaged version 2 consumer examples passed.");

void Validate(Order item) { (item.Total < 0).IfTrue.Throw<OrderException>(() => new(item.Id, "Negative total")); validated++; }
Receipt CreateReceipt(Order item) => new(item.Id, item.Total);
string BuildMessage() { messageCalls++; return "Rejected"; }
async Task SaveAsync(Order item, CancellationToken token)
{
    token.ThrowIfCancellationRequested();
    await Task.Yield();
    saved.Add(item.Id);
}
static void Check(bool condition, string scenario)
{
    if (!condition) throw new InvalidOperationException($"Consumer example failed: {scenario}");
}

sealed record Order(int Id, decimal Total, string Channel);
sealed record Receipt(int Id, decimal Total);
sealed class OrderException(int orderId, string message) : Exception(message) { public int OrderId { get; } = orderId; }
interface INotifier { string Describe(Receipt receipt); }
sealed class EmailNotifier : INotifier { public string Describe(Receipt receipt) => $"Email receipt {receipt.Id}"; }
sealed class SmsNotifier : INotifier { public string Describe(Receipt receipt) => $"SMS receipt {receipt.Id}"; }
