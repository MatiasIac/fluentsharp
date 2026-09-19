using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace FunctionalSharp.Patterns;

/// <summary>Creates typed constructor registries for factory/strategy selection.</summary>
public static class Factory
{
    /// <summary>Starts a registry with an optional key comparer.</summary>
    public static FactoryBuilder<TKey, TValue> For<TKey, TValue>(IEqualityComparer<TKey>? comparer = null) where TKey : notnull => new(comparer);
}

/// <summary>A mutable construction stage. Build creates an independent immutable registry snapshot.</summary>
public sealed class FactoryBuilder<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, Func<TValue>> constructors;
    internal FactoryBuilder(IEqualityComparer<TKey>? comparer) => constructors = new(comparer);

    /// <summary>Registers a unique key and constructor. Duplicate keys are rejected immediately.</summary>
    public FactoryBuilder<TKey, TValue> Register(TKey key, Func<TValue> constructor)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(constructor);
        if (!constructors.TryAdd(key, constructor))
            throw new ArgumentException($"A constructor is already registered for key '{key}'.", nameof(key));
        return this;
    }

    /// <summary>Copies the registrations without constructing any values.</summary>
    public Factory<TKey, TValue> Build() => new(new(constructors, constructors.Comparer));
}

/// <summary>An immutable key-to-constructor registry. Each successful Create calls the constructor anew.</summary>
/// <remarks>The caller owns returned objects, caching, disposal, and constructor thread safety.</remarks>
public sealed class Factory<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, Func<TValue>> constructors;
    internal Factory(Dictionary<TKey, Func<TValue>> constructors) => this.constructors = constructors;

    /// <summary>Constructs a value for a known key; throws KeyNotFoundException for an unknown key.</summary>
    public TValue Create(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return constructors.TryGetValue(key, out var constructor) ? constructor()
            : throw new KeyNotFoundException($"No constructor is registered for key '{key}'.");
    }

    /// <summary>Returns false for an unknown key. Exceptions thrown by registered constructors propagate.</summary>
    public bool TryCreate(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (constructors.TryGetValue(key, out var constructor))
        {
            value = constructor();
            return true;
        }
        value = default;
        return false;
    }
}
