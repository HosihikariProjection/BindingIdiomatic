namespace Hosihikari.Binding.Bedrock;

/// <summary>A chunk position.</summary>
/// <remarks>
/// The borrowed half. To create one, use <see cref="OwnedChunkPos"/>. A borrowed position
/// cannot be preserved: the projected surface has no factory or copy constructor for one,
/// only conversions from a block position or a vector.
/// </remarks>
public sealed class ChunkPos : IInteropBridge<InteropChunkPos>
{
    private InteropChunkPos? _interop;

    private ChunkPos(InteropChunkPos interop) => _interop = interop;

    /// <summary>Bridges an Interop wrapper.</summary>
    public static ChunkPos From(InteropChunkPos interop)
    {
        ArgumentNullException.ThrowIfNull(interop);
        return new ChunkPos(interop);
    }

    /// <inheritdoc/>
    public InteropChunkPos Interop => _interop ?? throw new ObjectDisposedException(nameof(ChunkPos));

    /// <summary>The native handle, or zero once the instance was invalidated.</summary>
    public nint Handle => _interop?.Handle ?? 0;

    /// <summary>The X component.</summary>
    public int X => Interop.x;

    /// <summary>The Z component.</summary>
    public int Z => Interop.z;

    /// <summary>Whether two positions are equal.</summary>
    public static bool operator ==(ChunkPos? left, ChunkPos? right) =>
        left is null ? right is null : right is not null && left.X == right.X && left.Z == right.Z;

    /// <summary>Whether two positions differ.</summary>
    public static bool operator !=(ChunkPos? left, ChunkPos? right) => !(left == right);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ChunkPos other && this == other;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(X, Z);

    /// <summary>Invalidates this instance. The native position is untouched.</summary>
    public void Dispose() => _interop = null;
}
