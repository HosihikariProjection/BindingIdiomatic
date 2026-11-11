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
    /// component properties keep working.
    /// </remarks>
    public Vec3? Center => Wrap(Interop.center());

    private static Vec3? Wrap(InteropVec3? value) => value is null ? null : Vec3.From(value);

    /// <summary>The block position's bottom centre as a vector.</summary>
    public Vec3? BottomCenter => Wrap(Interop.bottomCenter());

    /// <summary>Converts to a bounding box, or <see langword="null"/> when the carrier cannot.</summary>
    public AABB? ToAabb()
    {
        var value = Interop.operator_AABB();
        return value is null ? null : AABB.From(value);
    }

    /// <inheritdoc/>
    public override string ToString() => Interop.toString();

    /// <summary>Invalidates this instance. The native position is untouched.</summary>
    public void Dispose() => _interop = null;
}
