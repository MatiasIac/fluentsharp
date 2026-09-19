using System;
using System.Threading;
using System.Threading.Tasks;

namespace FunctionalSharp.Patterns;

/// <summary>An immutable asynchronous processing definition with sequential awaited steps.</summary>
/// <remarks>Observers report an already selected terminal outcome. They are awaited outside
/// retry and cancellation checks; observer errors fault the returned task.</remarks>
public sealed class AsyncPipeline<T>
{
    private sealed record Step(Func<T, CancellationToken, Task<T>>? Action, Func<T, bool>? Stop, RetryPolicy? Retry);
    private readonly Step[] steps;
    private readonly Func<T, Task>? completed;
    private readonly Func<T, Exception, Task>? failed;

    private AsyncPipeline(Step[] steps, Func<T, Task>? completed = null, Func<T, Exception, Task>? failed = null)
    {
        this.steps = steps;
        this.completed = completed;
        this.failed = failed;
    }

    /// <summary>Creates an empty asynchronous definition.</summary>
    public static AsyncPipeline<T> Create() => new([]);

    /// <summary>Appends an awaited payload transformation.</summary>
    public AsyncPipeline<T> Then(Func<T, Task<T>> step, RetryPolicy? retry = null)
    {
        ArgumentNullException.ThrowIfNull(step);
        return Then((value, token) => step(value), retry);
    }

    /// <summary>Appends a token-aware transformation and optional retry policy.</summary>
    public AsyncPipeline<T> Then(Func<T, CancellationToken, Task<T>> step, RetryPolicy? retry = null)
    {
        ArgumentNullException.ThrowIfNull(step);
        return new([.. steps, new(step, null, retry)], completed, failed);
    }

    /// <summary>Appends a synchronous condition that stops when true, retaining the payload.</summary>
    public AsyncPipeline<T> StopWhen(Func<T, bool> condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        return new([.. steps, new(null, condition, null)], completed, failed);
    }

    /// <summary>Observes completion. Observer exceptions fault the task without retries.</summary>
    public AsyncPipeline<T> OnCompleted(Func<T, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return new(steps, action, failed);
    }

    /// <summary>Observes an exhausted failure once. Observer exceptions fault the task.</summary>
    public AsyncPipeline<T> OnError(Func<T, Exception, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return new(steps, completed, action);
    }

    /// <summary>Awaits all running work, including during cancellation, and returns an explicit run result.</summary>
    public async Task<ExecutionResult<T>> RunAsync(T payload, CancellationToken cancellationToken = default)
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
                    var task = step.Retry is null ? step.Action!(current, cancellationToken)
                        : step.Retry.ExecuteAsync(async token =>
                        {
                            var stepTask = step.Action!(current, token)
                                ?? throw new InvalidOperationException("The step returned a null task.");
                            // Preserve the returned payload before the policy's cancellation check.
                            current = await stepTask.ConfigureAwait(false);
                            return current;
                        }, cancellationToken);
                    current = await (task ?? throw new InvalidOperationException("The step returned a null task.")).ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }
        catch (OperationCanceledException error) { return new(ExecutionStatus.Cancelled, current, stepIndex, error); }
        catch (Exception error)
        {
            if (failed is not null) await (failed(current, error) ?? throw new InvalidOperationException("The observer returned a null task.")).ConfigureAwait(false);
            return new(ExecutionStatus.Failed, current, stepIndex, error);
        }
        if (completed is not null) await (completed(current) ?? throw new InvalidOperationException("The observer returned a null task.")).ConfigureAwait(false);
        return new(ExecutionStatus.Completed, current);
    }
}
