using System.Reflection;
using System.Runtime.CompilerServices;

namespace Hosihikari.Binding;

/// <summary>
/// A set of subscriptions and owned handles reclaimed together.
/// </summary>
/// <remarks>
/// The scope exists because a handler attached to a shared native object pins the load context.
/// Every subscription made through a scope is registered here and dropped on dispose, so the
/// caller does not have to remember each one. A subscription without any scope throws instead
/// of pretending it will be collected.
/// <para>
/// Disposing is idempotent. The scope is not thread-affine itself, but subscriptions made on a
/// server thread are expected to be released there.
/// </para>
/// </remarks>
public sealed class BindingScope : IDisposable
{
    private static readonly ConditionalWeakTable<Assembly, BindingScope> Defaults = new();

    private readonly List<IDisposable> _reclaimables = [];
    private readonly Lock _gate = new();
    private bool _disposed;

    /// <summary>The registry this scope owns, or <see langword="null"/> when identity is not tracked.</summary>
    public IdentityRegistry<object>? Identities { get; init; }

    /// <summary>The generation source used to validate identity entries.</summary>
    public IGenerationSource? Generations { get; init; }

    /// <summary>Raised for each handler exception that a facade isolated.</summary>
    public event Action<BindingError>? Unhandled;

    /// <summary>Whether <see cref="Dispose"/> already ran.</summary>
    public bool IsDisposed => _disposed;

    /// <summary>
    /// Returns the default scope for <paramref name="assembly"/>, creating an open one on first use.
    /// </summary>
    /// <remarks>
    /// A default scope lives as long as its load context, which is the right lifetime for a plugin
    /// that never creates one explicitly. Callers that collect earlier should create their own.
    /// </remarks>
    public static BindingScope For(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        return Defaults.GetValue(assembly, static _ => new BindingScope());
    }

    /// <summary>Replaces the default scope for <paramref name="assembly"/>.</summary>
    public static void SetDefault(Assembly assembly, BindingScope scope)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(scope);
        Defaults.Remove(assembly);
        Defaults.Add(assembly, scope);
    }

    /// <summary>Resolves the scope to register against, throwing when none is live.</summary>
    /// <param name="scope">An explicit scope, or <see langword="null"/> to use the caller assembly's default.</param>
    public static BindingScope Require(BindingScope? scope)
    {
        var resolved = scope ?? For(Assembly.GetCallingAssembly());
        return resolved._disposed
            ? throw new InvalidOperationException(
                "The BindingScope is already disposed. Create a new one, or pass a live scope explicitly.")
            : resolved;
    }

    /// <summary>Registers a reclaimable and returns it unchanged.</summary>
    /// <remarks>
    /// When the scope is already disposed, the reclaimable is disposed immediately. That keeps
    /// "subscribe after release" from leaking instead of failing.
    /// </remarks>
    public T Track<T>(T reclaimable) where T : IDisposable
    {
        ArgumentNullException.ThrowIfNull(reclaimable);

        lock (_gate)
        {
            if (!_disposed)
            {
                _reclaimables.Add(reclaimable);
                return reclaimable;
            }
        }

        reclaimable.Dispose();
        return reclaimable;
    }

    /// <summary>Unregisters a reclaimable that was already released by the caller.</summary>
    public void Untrack(IDisposable reclaimable)
    {
        ArgumentNullException.ThrowIfNull(reclaimable);

        lock (_gate)
        {
            _reclaimables.Remove(reclaimable);
        }
    }

    /// <summary>Forwards an isolated handler exception to <see cref="Unhandled"/> listeners.</summary>
    public void Report(BindingError error)
    {
        if (_disposed)
        {
            return;
        }

        Unhandled?.Invoke(error);
    }

    /// <summary>Disposes every registered reclaimable and closes the scope.</summary>
    public void Dispose()
    {
        IDisposable[] snapshot;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            snapshot = [.. _reclaimables];
            _reclaimables.Clear();
        }

        // Reverse order: later subscriptions usually depend on earlier ones.
        for (var index = snapshot.Length - 1; index >= 0; index--)
        {
            snapshot[index].Dispose();
        }

        Identities?.Clear();
    }
}
