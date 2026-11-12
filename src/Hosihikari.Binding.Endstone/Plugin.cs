using InteropPlugin = Hosihikari.Bindings.Endstone.Interop.endstone.Plugin;

namespace Hosihikari.Binding.Endstone;

/// <summary>A plugin as the server knows it.</summary>
/// <remarks>
/// Reached by borrowing only: a plugin is owned by the server's plugin manager.
/// </remarks>
public sealed class Plugin : IInteropBridge<InteropPlugin>
{
    private InteropPlugin? _interop;

    private Plugin(InteropPlugin interop) => _interop = interop;

    /// <summary>Bridges an Interop wrapper, reusing the instance already registered for the target.</summary>
    /// <param name="interop">The wrapper to bridge.</param>
    /// <param name="scope">The scope owning the identity registry.</param>
    public static Plugin From(InteropPlugin interop, BindingScope scope)
    {
        ArgumentNullException.ThrowIfNull(interop);
        var identities = BindingScope.Require(scope).Identities
            ?? throw new InvalidOperationException("The scope does not track identities.");

        var generation = scope.Generations?.GenerationOf(interop.Handle) ?? 0;
        var existing = identities.TryGet<Plugin>(interop.Handle, generation);
        if (existing is not null)
        {
            existing._interop = interop;
            return existing;
        }

        var created = new Plugin(interop);
        identities.GetOrAdd(interop.Handle, generation, () => created);
        return created;
    }

    /// <inheritdoc/>
    public InteropPlugin Interop => _interop ?? throw new ObjectDisposedException(nameof(Plugin));

    /// <summary>The native handle, or zero once the instance was invalidated.</summary>
    public nint Handle => _interop?.Handle ?? 0;

    /// <summary>The plugin's name.</summary>
    public string Name => Interop.getName();

    /// <summary>Invalidates this instance. The native plugin is untouched.</summary>
    public void Dispose() => _interop = null;
}
