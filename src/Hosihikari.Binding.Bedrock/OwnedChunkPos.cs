namespace Hosihikari.Binding.Bedrock;

/// <summary>A chunk position, owned by the caller.</summary>
/// <remarks>
/// The owned half of the chunk-position pair: the shim has real factories for it, so it is
/// created here and destroyed on dispose. The borrowed counterpart arrives from another object.
/// </remarks>
public sealed class OwnedChunkPos :
    IDisposable,
    IInteropBridge<InteropChunkPos>
{
    private InteropChunkPos? _interop;

    private OwnedChunkPos(InteropChunkPos interop) => _interop = interop;

    /// <summary>Creates a chunk position from a block position.</summary>
    public OwnedChunkPos(BlockPos position) : this(CreateFrom(position)) { }

    /// <summary>Creates a chunk position from a vector.</summary>
    public OwnedChunkPos(Vec3 position) : this(CreateFrom(position)) { }

    private static InteropChunkPos CreateFrom(BlockPos position)
    {
        ArgumentNullException.ThrowIfNull(position);
        return Factories.Require(InteropChunkPos.Create(position.Interop), "a chunk position");
    }

    private static InteropChunkPos CreateFrom(Vec3 position)
    {
        ArgumentNullException.ThrowIfNull(position);
        return Factories.Require(InteropChunkPos.Create(position.Interop), "a chunk position");
    }

    /// <summary>Copies a borrowed chunk position into an owned one.</summary>
    public static OwnedChunkPos CopyOf(ChunkPos other)
    {
        ArgumentNullException.ThrowIfNull(other);
        // CopyOf wraps the borrowed wrapper directly: Create only takes a BlockPos or a Vec3,
        // so rebuilding from the wrapper needs the converting constructor instead.
        return new OwnedChunkPos(new InteropChunkPos(other.Handle));
    }


    /// <inheritdoc/>
    public InteropChunkPos Interop => _interop ?? throw new ObjectDisposedException(nameof(OwnedChunkPos));

    /// <summary>The native handle, or zero once disposed.</summary>
    public nint Handle => _interop?.Handle ?? 0;

    /// <summary>The X component.</summary>
    public int X
    {
        get => Interop.x;
        set => Interop.x = value;
    }

    /// <summary>The Z component.</summary>
    public int Z
    {
        get => Interop.z;
        set => Interop.z = value;
    }

    /// <summary>Destroys the native chunk position.</summary>
    public void Dispose()
    {
        var interop = _interop;
        if (interop is null)
        {
            return;
        }

        _interop = null;
        interop.Dispose();
    }
}
