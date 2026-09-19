using System;

namespace FunctionalSharp.Operations;

/// <summary>A captured boolean condition. The default value is inactive.</summary>
public readonly struct Condition(bool isMatched)
{
    /// <summary>Whether this condition enables its actions.</summary>
    public bool IsMatched { get; } = isMatched;

    /// <summary>Runs an action once when matched and returns this condition.</summary>
    public Condition Then(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (IsMatched) action();
        return this;
    }

    /// <summary>Evaluates exactly one branch and returns its result.</summary>
    public TResult Match<TResult>(Func<TResult> whenTrue, Func<TResult> whenFalse)
    {
        ArgumentNullException.ThrowIfNull(whenTrue);
        ArgumentNullException.ThrowIfNull(whenFalse);
        return IsMatched ? whenTrue() : whenFalse();
    }

    /// <summary>Creates and throws an exception only when matched.</summary>
    /// <remarks>A null factory is always invalid; a matched factory must return an exception.</remarks>
    public void Throw<TException>(Func<TException> exceptionFactory) where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(exceptionFactory);
        if (IsMatched)
            throw (Exception?)exceptionFactory() ?? new InvalidOperationException("The exception factory returned null.");
    }

    /// <summary>Uses the exception's public parameterless constructor only when matched.</summary>
    public void Throw<TException>() where TException : Exception, new()
    {
        if (IsMatched) throw new TException();
    }
}
