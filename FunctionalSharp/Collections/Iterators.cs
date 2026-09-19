using System;
using System.Collections.Generic;

namespace FunctionalSharp.Collections;

/// <summary>Immediate sequential collection actions, with first-false stopping.</summary>
public static class Iterators
{
    /// <summary>Executes the action once for every item in enumeration order.</summary>
    public static void ForEvery<T>(this IEnumerable<T> collection, Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(action);
        foreach (var item in collection) action(item);
    }

    /// <summary>Checks a zero-based indexed condition before each action; stops at its first false result.</summary>
    public static void For<T>(this IEnumerable<T> collection, Func<T, int, bool> condition, Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(action);
        var index = 0;
        foreach (var item in collection)
        {
            if (!condition(item, index++)) break;
            action(item);
        }
    }

    /// <summary>Checks the condition before each action; stops at its first false result.</summary>
    public static void For<T>(this IEnumerable<T> collection, Func<T, bool> condition, Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(action);
        foreach (var item in collection)
        {
            if (!condition(item)) break;
            action(item);
        }
    }

    /// <summary>Executes each callback and stops after the first callback returning false.</summary>
    public static void For<T>(this IEnumerable<T> collection, Func<T, bool> action)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(action);
        foreach (var item in collection)
            if (!action(item)) break;
    }
}
