namespace Hosihikari.Binding.Endstone;

/// <summary>A dimension level.</summary>
/// <remarks>
/// Levels are reached by borrowing only; the shim emits no disposer for host-owned Endstone
/// objects. The instance stays valid while the server keeps the level loaded.
/// </remarks>
public sealed class Level : IInteropBridge<InteropLevel>
{
    private InteropLevel? _interop;

    private Level(InteropLevel interop) => _interop = interop;

    /// <summary>Bridges an Interop wrapper, reusing the instance already registered for the level.</summary>
    public static Level From(InteropLevel interop, BindingScope scope)
    {
        ArgumentNullException.ThrowIfNull(interop);
        var identities = BindingScope.Require(scope).Identities
            ?? throw new InvalidOperationException("The scope does not track identities.");

        var generation = scope.Generations?.GenerationOf(interop.Handle) ?? 0;
        var existing = identities.TryGet<Level>(interop.Handle, generation);
        if (existing is not null)
        {
            return existing;
        }

        var created = new Level(interop);
        identities.GetOrAdd(interop.Handle, generation, () => created);
        return created;
    }

    /// <inheritdoc/>
    public InteropLevel Interop => _interop ?? throw new ObjectDisposedException(nameof(Level));

    /// <summary>The native handle, or zero once the instance was invalidated.</summary>
    public nint Handle => _interop?.Handle ?? 0;

    /// <summary>The level name.</summary>
    public string Name => Interop.getName();

    /// <summary>The level seed.</summary>
    public long Seed => Interop.getSeed();

    /// <summary>The current level time.</summary>
    public int Time
    {
        get => Interop.getTime();
        set => Interop.setTime(value);
    }

    /// <summary>The actors in the level.</summary>
    /// <remarks>Raw handles for now; actor wrappers are a later batch.</remarks>
    public nint[] Actors => Interop.getActors();

    /// <summary>The dimensions in the level.</summary>
    public nint[] Dimensions => Interop.getDimensions();

    /// <summary>Looks a dimension up by name.</summary>
    /// <param name="name">The dimension name.</param>
    public Dimension? this[string name] => Interop.getDimension(name);

    /// <summary>Invalidates this instance. The native level is untouched.</summary>
    public void Dispose() => _interop = null;
}
