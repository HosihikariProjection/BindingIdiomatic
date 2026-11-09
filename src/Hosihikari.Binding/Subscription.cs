namespace Hosihikari.Binding;

/// <summary>An exception a facade isolated from a handler.</summary>
/// <param name="Handler">The delegate that threw.</param>
/// <param name="Exception">The exception it threw.</param>
public readonly record struct BindingError(Delegate Handler, Exception Exception);

/// <summary>A subscription a scope reclaims.</summary>
/// <remarks>
/// Disposing only drops this one subscription; the scope may still hold others.
/// Disposing twice is a no-op.
/// </remarks>
public sealed class Subscription : IDisposable
{
    private Action? _unsubscribe;

    internal Subscription(Action unsubscribe) => _unsubscribe = unsubscribe;

    /// <summary>Whether the subscription is still live.</summary>
    public bool IsActive => _unsubscribe is not null;

    /// <inheritdoc/>
    public void Dispose() => Interlocked.Exchange(ref _unsubscribe, null)?.Invoke();
}

/// <summary>
/// Runs a handler chain with per-handler isolation and reports failures to a scope.
/// </summary>
/// <remarks>
/// One throwing handler must not poison the rest of a tick, so each invocation is wrapped
/// individually and the failure is forwarded to <see cref="BindingScope.Report"/> instead of
/// propagating to the native caller.
/// </remarks>
public static class EventFiring
{
    /// <summary>Invokes a single handler, reporting a failure instead of throwing.</summary>
    public static void Fire<TArg>(BindingScope? scope, Delegate handler, TArg argument)
    {
        try
        {
            ((Action<TArg>)handler)(argument);
        }
        catch (Exception exception)
        {
            scope?.Report(new BindingError(handler, exception));
        }
    }

    /// <summary>Invokes a handler list on a snapshot, reporting each failure.</summary>
    public static void FireAll<TArg>(BindingScope? scope, List<Delegate> handlers, TArg argument)
    {
        ArgumentNullException.ThrowIfNull(handlers);

        Delegate[] snapshot;
        lock (handlers)
        {
            snapshot = [.. handlers];
        }

        foreach (var handler in snapshot)
        {
            Fire(scope, handler, argument);
        }
    }
}
