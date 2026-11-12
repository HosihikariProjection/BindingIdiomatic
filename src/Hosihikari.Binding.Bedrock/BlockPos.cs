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

    /// <summary>
    /// The block position's centre as a vector, or <see langword="null"/> when the carrier
    /// does not export the accessor.
    /// </summary>
    /// <remarks>
    /// <c>BlockPos::center</c> is annotated MCNAPI in the carrier header and absent from the
    /// run-time data of the current build, so a bound wrapper can fail here while the
    /// component properties keep working. The shim allocates what it returns, so dispose it.
    /// </remarks>
    public OwnedVec3? Center => Adopt(Interop.center());

    private static OwnedVec3? Adopt(InteropVec3? value) => value is null ? null : OwnedVec3.Adopt(value);

    /// <summary>The block position's bottom centre as a vector; dispose it.</summary>
    public OwnedVec3? BottomCenter => Adopt(Interop.bottomCenter());

    /// <summary>
    /// Converts to a bounding box, or <see langword="null"/> when the carrier cannot.
    /// </summary>
    /// <remarks>
    /// The shim allocates the box, so the result is owned: dispose it.
    /// </remarks>
    public OwnedAabb? ToAabb()
    {
        var value = Interop.operator_AABB();
        return value is null ? null : OwnedAabb.Adopt(value);
    }

    /// <summary>
    /// Converts to a block-aligned box, or <see langword="null"/> when the carrier cannot.
    /// </summary>
    /// <remarks>
    /// The shim allocates the box, so the result is owned: dispose it.
    /// </remarks>
    public OwnedBoundingBox? ToBoundingBox()
    {
        var value = Interop.operator_BoundingBox();
        return value is null ? null : OwnedBoundingBox.Adopt(value);
    }

    /// <inheritdoc/>
    public override string ToString() => Interop.toString();

    /// <summary>Invalidates this instance. The native position is untouched.</summary>
    public void Dispose() => _interop = null;
}
