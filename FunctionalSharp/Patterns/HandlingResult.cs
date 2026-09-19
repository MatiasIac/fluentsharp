using System;

namespace FunctionalSharp.Patterns;

/// <summary>The terminal outcome of a chain of responsibility.</summary>
public enum HandlingStatus
{
    /// <summary>A matching handler or fallback returned a response.</summary>
    Handled,
    /// <summary>No handler matched and no fallback was provided.</summary>
    Unhandled,
    /// <summary>Cancellation interrupted handling.</summary>
    Cancelled,
    /// <summary>A predicate or handler failed.</summary>
    Failed
}

/// <summary>A response or an explicit unhandled, cancelled, or failed outcome.</summary>
public sealed class HandlingResult<T>
{
    private readonly T value;

    internal HandlingResult(HandlingStatus status, T value = default!, int? handlerIndex = null, Exception? error = null)
    {
        Status = status;
        this.value = value;
        HandlerIndex = handlerIndex;
        Error = error;
    }

    /// <summary>The terminal outcome.</summary>
    public HandlingStatus Status { get; }
    /// <summary>The returned response, possibly null if T permits it. Throws unless Status is Handled.</summary>
    public T Value => Status == HandlingStatus.Handled ? value : throw new InvalidOperationException("The request has no handled response.");
    /// <summary>The selected/current zero-based handler index. The fallback uses the number of registered handlers.</summary>
    public int? HandlerIndex { get; }
    /// <summary>The original failure or cancellation exception, when present.</summary>
    public Exception? Error { get; }
}
