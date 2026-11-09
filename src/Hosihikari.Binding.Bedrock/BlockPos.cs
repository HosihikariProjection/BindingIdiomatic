using InteropBlockPos = Hosihikari.Bindings.Bedrock.Interop.BlockPos;

namespace Hosihikari.Binding.Bedrock;

/// <summary>A block position.</summary>
/// <remarks>
/// Reached by borrowing only. The neighbouring-accessor family is not exported by the carrier, so
/// it stays on the Interop wrapper as a stub; the components and the conversions that do work are
/// here.
/// </remarks>
public sealed class BlockPos : IInteropBridge<InteropBlockPos>
{
    private InteropBlockPos? _interop;

    private BlockPos(InteropBlockPos interop) => _interop = interop;

    /// <summary>Bridges an Interop wrapper.</summary>
    public static BlockPos From(InteropBlockPos interop)
    {
        ArgumentNullException.ThrowIfNull(interop);
        return new BlockPos(interop);
    }

    /// <inheritdoc/>
    public InteropBlockPos Interop => _interop ?? throw new ObjectDisposedException(nameof(BlockPos));

    /// <summary>The native handle, or zero once the instance was invalidated.</summary>
    public nint Handle => _interop?.Handle ?? 0;

    /// <summary>The X component.</summary>
    public int X
    {
        get => Interop.x;
        set => Interop.x = value;
    }

    /// <summary>The Y component.</summary>
    public int Y
    {
        get => Interop.y;
        set => Interop.y = value;
    }

    /// <summary>The Z component.</summary>
    public int Z
    {
        get => Interop.z;
        set => Interop.z = value;
    }

    /// <summary>The block position's centre as a vector.</summary>
    public Vec3 Center => Vec3.From(Interop.center());

    /// <summary>The block position's bottom centre as a vector.</summary>
    public Vec3 BottomCenter => Vec3.From(Interop.bottomCenter());

    /// <summary>Converts to a bounding box.</summary>
    public AABB ToAabb() => AABB.From(Interop.operator_AABB());

    /// <inheritdoc/>
    public override string ToString() => Interop.toString();

    /// <summary>Invalidates this instance. The native position is untouched.</summary>
    public void Dispose() => _interop = null;
}
