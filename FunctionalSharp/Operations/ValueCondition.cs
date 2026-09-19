using System;

namespace FunctionalSharp.Operations;

/// <summary>A captured non-null value with conditional typed callbacks. Default is inactive.</summary>
public readonly struct ValueCondition<T> where T : notnull
{
    private readonly T value;

    internal ValueCondition(T value, bool isMatched)
    {
        this.value = value;
        IsMatched = isMatched;
    }

    /// <summary>Whether a non-null value was captured.</summary>
    public bool IsMatched { get; }

    /// <summary>Runs the action with the captured non-null value when matched.</summary>
    public ValueCondition<T> Then(Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (IsMatched) action(value);
        return this;
    }

    /// <summary>Evaluates the matching value branch or the lazy fallback, once.</summary>
    public TResult Match<TResult>(Func<T, TResult> whenNotNull, Func<TResult> whenNull)
    {
        ArgumentNullException.ThrowIfNull(whenNotNull);
        ArgumentNullException.ThrowIfNull(whenNull);
        return IsMatched ? whenNotNull(value) : whenNull();
    }

    /// <summary>Creates an exception from the non-null value and throws it only when matched.</summary>
    public void Throw<TException>(Func<T, TException> exceptionFactory) where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(exceptionFactory);
        if (IsMatched)
            throw (Exception?)exceptionFactory(value) ?? new InvalidOperationException("The exception factory returned null.");
    }
}
