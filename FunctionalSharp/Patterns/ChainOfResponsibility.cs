using System;
using System.Threading;

namespace FunctionalSharp.Patterns;

/// <summary>An immutable, ordered first-match handler chain with an optional fallback.</summary>
public sealed class ChainOfResponsibility<TRequest, TResponse>
{
    private sealed record Handler(Func<TRequest, bool> Matches, Func<TRequest, CancellationToken, TResponse> Handle);
    private readonly Handler[] handlers;
    private readonly Func<TRequest, CancellationToken, TResponse>? fallback;

    private ChainOfResponsibility(Handler[] handlers, Func<TRequest, CancellationToken, TResponse>? fallback = null)
    {
        this.handlers = handlers;
        this.fallback = fallback;
    }

    /// <summary>Creates an empty chain.</summary>
    public static ChainOfResponsibility<TRequest, TResponse> Create() => new([]);

    /// <summary>Appends a predicate and its handler. Only the first match handles the request.</summary>
    public ChainOfResponsibility<TRequest, TResponse> When(Func<TRequest, bool> predicate, Func<TRequest, TResponse> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return When(predicate, (request, token) => handler(request));
    }

    /// <summary>Appends a predicate and token-aware handler.</summary>
    public ChainOfResponsibility<TRequest, TResponse> When(Func<TRequest, bool> predicate, Func<TRequest, CancellationToken, TResponse> handler)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(handler);
        return new([.. handlers, new(predicate, handler)], fallback);
    }

    /// <summary>Sets the fallback for requests with no matching predicate.</summary>
    public ChainOfResponsibility<TRequest, TResponse> Otherwise(Func<TRequest, TResponse> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return Otherwise((request, token) => handler(request));
    }

    /// <summary>Sets a token-aware fallback. It does not handle failures or cancellation.</summary>
    public ChainOfResponsibility<TRequest, TResponse> Otherwise(Func<TRequest, CancellationToken, TResponse> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return new(handlers, handler);
    }

    /// <summary>Handles a request once and returns its response or an explicit terminal outcome.</summary>
    public HandlingResult<TResponse> Handle(TRequest request, CancellationToken cancellationToken = default)
    {
        int? currentIndex = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var index = 0; index < handlers.Length; index++)
            {
                currentIndex = index;
                cancellationToken.ThrowIfCancellationRequested();
                var matches = handlers[index].Matches(request);
                cancellationToken.ThrowIfCancellationRequested();
                if (!matches) continue;
                var response = handlers[index].Handle(request, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                return new(HandlingStatus.Handled, response, index);
            }
            if (fallback is not null)
            {
                currentIndex = handlers.Length;
                cancellationToken.ThrowIfCancellationRequested();
                var response = fallback(request, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                return new(HandlingStatus.Handled, response, currentIndex);
            }
            return new(HandlingStatus.Unhandled);
        }
        catch (OperationCanceledException error) { return new(HandlingStatus.Cancelled, handlerIndex: currentIndex, error: error); }
        catch (Exception error) { return new(HandlingStatus.Failed, handlerIndex: currentIndex, error: error); }
    }
}
