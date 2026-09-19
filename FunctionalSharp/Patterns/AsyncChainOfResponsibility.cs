using System;
using System.Threading;
using System.Threading.Tasks;

namespace FunctionalSharp.Patterns;

/// <summary>An immutable first-match chain that awaits its predicates and handlers in order.</summary>
public sealed class AsyncChainOfResponsibility<TRequest, TResponse>
{
    private sealed record Handler(Func<TRequest, CancellationToken, Task<bool>> Matches, Func<TRequest, CancellationToken, Task<TResponse>> Handle);
    private readonly Handler[] handlers;
    private readonly Func<TRequest, CancellationToken, Task<TResponse>>? fallback;

    private AsyncChainOfResponsibility(Handler[] handlers, Func<TRequest, CancellationToken, Task<TResponse>>? fallback = null)
    {
        this.handlers = handlers;
        this.fallback = fallback;
    }

    /// <summary>Creates an empty asynchronous chain.</summary>
    public static AsyncChainOfResponsibility<TRequest, TResponse> Create() => new([]);

    /// <summary>Appends a synchronous predicate and task-returning handler.</summary>
    public AsyncChainOfResponsibility<TRequest, TResponse> When(Func<TRequest, bool> predicate, Func<TRequest, Task<TResponse>> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return When(predicate, (request, token) => handler(request));
    }

    /// <summary>Appends a synchronous predicate and a token-aware task-returning handler.</summary>
    public AsyncChainOfResponsibility<TRequest, TResponse> When(Func<TRequest, bool> predicate, Func<TRequest, CancellationToken, Task<TResponse>> handler)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return When((request, token) => Task.FromResult(predicate(request)), handler);
    }

    /// <summary>Appends an awaited predicate and handler, both receiving the cancellation token.</summary>
    public AsyncChainOfResponsibility<TRequest, TResponse> When(Func<TRequest, CancellationToken, Task<bool>> predicate, Func<TRequest, CancellationToken, Task<TResponse>> handler)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(handler);
        return new([.. handlers, new(predicate, handler)], fallback);
    }

    /// <summary>Sets an awaited fallback for unmatched requests.</summary>
    public AsyncChainOfResponsibility<TRequest, TResponse> Otherwise(Func<TRequest, Task<TResponse>> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return Otherwise((request, token) => handler(request));
    }

    /// <summary>Sets a token-aware fallback. Failures and cancellation do not invoke it.</summary>
    public AsyncChainOfResponsibility<TRequest, TResponse> Otherwise(Func<TRequest, CancellationToken, Task<TResponse>> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return new(handlers, handler);
    }

    /// <summary>Awaits the first matching handler or fallback and returns an explicit outcome.</summary>
    public async Task<HandlingResult<TResponse>> HandleAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        int? currentIndex = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var index = 0; index < handlers.Length; index++)
            {
                currentIndex = index;
                cancellationToken.ThrowIfCancellationRequested();
                var predicateTask = handlers[index].Matches(request, cancellationToken)
                    ?? throw new InvalidOperationException("The predicate returned a null task.");
                var matches = await predicateTask.ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (!matches) continue;
                var task = handlers[index].Handle(request, cancellationToken)
                    ?? throw new InvalidOperationException("The handler returned a null task.");
                var response = await task.ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                return new(HandlingStatus.Handled, response, index);
            }
            if (fallback is not null)
            {
                currentIndex = handlers.Length;
                cancellationToken.ThrowIfCancellationRequested();
                var task = fallback(request, cancellationToken)
                    ?? throw new InvalidOperationException("The fallback returned a null task.");
                var response = await task.ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                return new(HandlingStatus.Handled, response, currentIndex);
            }
            return new(HandlingStatus.Unhandled);
        }
        catch (OperationCanceledException error) { return new(HandlingStatus.Cancelled, handlerIndex: currentIndex, error: error); }
        catch (Exception error) { return new(HandlingStatus.Failed, handlerIndex: currentIndex, error: error); }
    }
}
