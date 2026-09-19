using System;

namespace FunctionalSharp.Composition;

/// <summary>Immediate, typed value composition without implicit enumeration.</summary>
public static class Values
{
    /// <summary>Transforms the value once and returns the result. Null values are passed through.</summary>
    public static TResult Pipe<T, TResult>(this T value, Func<T, TResult> transform)
    {
        ArgumentNullException.ThrowIfNull(transform);
        return transform(value);
    }

    /// <summary>Observes the value once and returns the original value with its original static type.</summary>
    public static T Tap<T>(this T value, Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        action(value);
        return value;
    }
}
