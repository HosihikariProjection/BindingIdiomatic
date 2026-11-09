namespace Hosihikari.Binding.Endstone;

/// <summary>A UUID value.</summary>
/// <remarks>
/// Reached by borrowing; nothing owns a UUID. Reading the string form is the useful part and is
/// what <see cref="ToString"/> exposes.
/// </remarks>
public sealed class UUID : IInteropBridge<InteropUUID>
{
    private InteropUUID? _interop;

    private UUID(InteropUUID interop) => _interop = interop;

    /// <summary>Bridges an Interop wrapper, reusing the instance already registered for the id.</summary>
    public static UUID From(InteropUUID interop, BindingScope scope)
    {
        ArgumentNullException.ThrowIfNull(interop);
        var identities = BindingScope.Require(scope).Identities
            ?? throw new InvalidOperationException("The scope does not track identities.");

        var generation = scope.Generations?.GenerationOf(interop.Handle) ?? 0;
        var existing = identities.TryGet<UUID>(interop.Handle, generation);
        if (existing is not null)
        {
            return existing;
        }

        var created = new UUID(interop);
        identities.GetOrAdd(interop.Handle, generation, () => created);
        return created;
    }

    /// <inheritdoc/>
    public InteropUUID Interop => _interop ?? throw new ObjectDisposedException(nameof(UUID));

    /// <summary>The native handle, or zero once the instance was invalidated.</summary>
    public nint Handle => _interop?.Handle ?? 0;

    /// <summary>Whether this is the nil UUID.</summary>
    public bool IsNil => Interop.isNil();

    /// <summary>The UUID version.</summary>
    public int Version => Interop.version();

    /// <summary>Swaps the value with another UUID in place.</summary>
    public void Swap(UUID other)
    {
        ArgumentNullException.ThrowIfNull(other);
        Interop.swap(other.Interop);
    }

    /// <summary>The canonical string form.</summary>
    public override string ToString() => Interop.str();

    /// <summary>Whether two ids are equal.</summary>
    public static bool operator ==(UUID? left, UUID? right) =>
        left is null ? right is null : right is not null && left.Interop.str() == right.Interop.str();

    /// <summary>Whether two ids differ.</summary>
    public static bool operator !=(UUID? left, UUID? right) => !(left == right);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is UUID other && this == other;

    /// <inheritdoc/>
    public override int GetHashCode() => ToString().GetHashCode(StringComparison.Ordinal);

    /// <summary>Invalidates this instance. The native id is untouched.</summary>
    public void Dispose() => _interop = null;
}
