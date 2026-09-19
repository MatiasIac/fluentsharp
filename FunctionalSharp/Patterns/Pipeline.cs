using System;
using System.Threading;

namespace FunctionalSharp.Patterns;

/// <summary>An immutable synchronous processing definition. Each Run receives its own payload.</summary>
/// <remarks>Definition reuse is safe; callers are responsible for thread-safe callbacks and independent mutable payloads.</remarks>
public sealed class Pipeline<T>
{
    private sealed record Step(Func<T, CancellationToken, T>? Action, Func<T, bool>? Stop, RetryPolicy? Retry);
    private readonly Step[] steps;
    private readonly Action<T>? completed;
    private readonly Action<T, Exception>? failed;

    private Pipeline(Step[] steps, Action<T>? completed = null, Action<T, Exception>? failed = null)
    {
        this.steps = steps;
        this.completed = completed;
        this.failed = failed;
    }

    /// <summary>Creates an empty definition.</summary>
    public static Pipeline<T> Create() => new([]);

    /// <summary>Returns a new definition with a payload transformation appended.</summary>
    public Pipeline<T> Then(Func<T, T> step, RetryPolicy? retry = null)
    {
        ArgumentNullException.ThrowIfNull(step);
        return Then((value, token) => step(value), retry);
    }

    /// <summary>Appends a token-aware transformation and an optional retry policy.</summary>
    public Pipeline<T> Then(Func<T, CancellationToken, T> step, RetryPolicy? retry = null)
    {
        ArgumentNullException.ThrowIfNull(step);
        return new([.. steps, new(step, null, retry)], completed, failed);
    }

    /// <summary>Appends a condition that stops successfully when true, retaining the current payload.</summary>
    public Pipeline<T> StopWhen(Func<T, bool> condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        return new([.. steps, new(null, condition, null)], completed, failed);
    }

    /// <summary>Returns a definition with a completion observer. Observer errors propagate without retries.</summary>
    public Pipeline<T> OnCompleted(Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return new(steps, action, failed);
    }

    /// <summary>Observes an exhausted failure once. Observer errors propagate; cancellation is not reported here.</summary>
    public Pipeline<T> OnError(Action<T, Exception> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return new(steps, completed, action);
    }

    /// <summary>Runs sequentially, returning a completed, stopped, cancelled, or failed result.</summary>
    public ExecutionResult<T> Run(T payload, CancellationToken cancellationToken = default)
    {
        var current = payload;
        int? stepIndex = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var index = 0; index < steps.Length; index++)
            {
                stepIndex = index;
                cancellationToken.ThrowIfCancellationRequested();
                var step = steps[index];
                if (step.Stop is not null)
                {
                    var stop = step.Stop(current);
                    cancellationToken.ThrowIfCancellationRequested();
                    if (stop) return new(ExecutionStatus.Stopped, current, index);
                }
                else
                {
                    // Retain a returned payload even if the retry policy then observes cancellation.
                    current = step.Retry is null ? step.Action!(current, cancellationToken)
                        : step.Retry.Execute(() => current = step.Action!(current, cancellationToken), cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }
        catch (OperationCanceledException error) { return new(ExecutionStatus.Cancelled, current, stepIndex, error); }
        catch (Exception error)
        {
            failed?.Invoke(current, error);
            return new(ExecutionStatus.Failed, current, stepIndex, error);
        }
        completed?.Invoke(current);
        return new(ExecutionStatus.Completed, current);
    }
}
