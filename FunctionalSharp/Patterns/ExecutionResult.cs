using System;

namespace FunctionalSharp.Patterns;

/// <summary>The terminal outcome of a pipeline run.</summary>
public enum ExecutionStatus
{
    /// <summary>All steps finished successfully.</summary>
    Completed,
    /// <summary>A stop condition matched.</summary>
    Stopped,
    /// <summary>Cancellation was requested or a step threw OperationCanceledException.</summary>
    Cancelled,
    /// <summary>A step failed after its permitted attempts.</summary>
    Failed
}

/// <summary>A run's final payload and outcome, independent of its reusable definition.</summary>
public sealed class ExecutionResult<T>
{
    internal ExecutionResult(ExecutionStatus status, T payload, int? stepIndex = null, Exception? error = null)
    {
        Status = status;
        Payload = payload;
        StepIndex = stepIndex;
        Error = error;
    }

    /// <summary>The terminal outcome.</summary>
    public ExecutionStatus Status { get; }
    /// <summary>The most recently returned payload. Reference mutations are not rolled back on failure.</summary>
    public T Payload { get; }
    /// <summary>The zero-based step at which execution stopped, failed, or was cancelled; otherwise null.</summary>
    public int? StepIndex { get; }
    /// <summary>The original failure or cancellation exception; null for completed or stopped runs.</summary>
    public Exception? Error { get; }
}
