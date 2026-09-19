using System;
using System.Threading;
using System.Threading.Tasks;

namespace FunctionalSharp.Patterns;

/// <summary>A bounded retry decorator with an explicit failure predicate and no implicit delay.</summary>
/// <remarks>Attempts include the first call. Cancellation is never retried. Retries do not roll back side effects.</remarks>
public sealed class RetryPolicy
{
    private readonly Func<Exception, bool> shouldRetry;

    /// <summary>Creates a policy with at least one attempt and an explicit retryable-failure predicate.</summary>
    public RetryPolicy(int maxAttempts, Func<Exception, bool> shouldRetry)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);
        ArgumentNullException.ThrowIfNull(shouldRetry);
        MaxAttempts = maxAttempts;
        this.shouldRetry = shouldRetry;
    }

    /// <summary>The maximum total calls, including the initial attempt.</summary>
    public int MaxAttempts { get; }

    /// <summary>Executes an operation, retrying only selected failures within the attempt limit.</summary>
    public T Execute<T>(Func<T> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        for (var attempt = 1; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var result = operation();
                cancellationToken.ThrowIfCancellationRequested();
                return result;
            }
            catch (Exception error) when (error is not OperationCanceledException && attempt < MaxAttempts)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!shouldRetry(error)) throw;
            }
        }
    }

    /// <summary>Executes a synchronous action under this policy.</summary>
    public void Execute(Action operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        Execute(() => { operation(); return true; }, cancellationToken);
    }

    /// <summary>Awaits every attempt. Cancellation never abandons an in-flight callback.</summary>
    public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return ExecuteCoreAsync(operation, cancellationToken);
    }

    /// <summary>Awaits an asynchronous action under this policy.</summary>
    public Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return ExecuteCoreAsync(async token =>
        {
            var task = operation(token) ?? throw new InvalidOperationException("The operation returned a null task.");
            await task.ConfigureAwait(false);
            return true;
        }, cancellationToken);
    }

    private async Task<T> ExecuteCoreAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var task = operation(cancellationToken) ?? throw new InvalidOperationException("The operation returned a null task.");
                var result = await task.ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                return result;
            }
            catch (Exception error) when (error is not OperationCanceledException && attempt < MaxAttempts)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!shouldRetry(error)) throw;
            }
        }
    }
}
