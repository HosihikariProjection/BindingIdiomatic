namespace Hosihikari.Binding.Bedrock;

/// <summary>A block-aligned bounding box the caller owns and must dispose.</summary>
/// <remarks>
/// The owned half of the box pair. The native box is destroyed on dispose.
/// </remarks>
public sealed class OwnedBoundingBox :
    IDisposable,
    IInteropBridge<InteropBoundingBox>
{
    private InteropBoundingBox? _interop;

    private OwnedBoundingBox(InteropBoundingBox interop) => _interop = interop;

    /// <summary>Creates a box spanning two corners.</summary>
    public OwnedBoundingBox(BlockPos min, BlockPos max)
    {
        ArgumentNullException.ThrowIfNull(min);
        ArgumentNullException.ThrowIfNull(max);
        _interop = Factories.Require(InteropBoundingBox.Create(min.Interop, max.Interop), "a bounding box");
    }

    /// <summary>Adopts a wrapper the shim already allocated.</summary>
    internal static OwnedBoundingBox Adopt(InteropBoundingBox interop) => new(interop);

    /// <summary>Copies a borrowed box into an owned one.</summary>
    public static OwnedBoundingBox CopyOf(BoundingBox other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new OwnedBoundingBox(Factories.Require(InteropBoundingBox.Create(other.Interop), "a copied bounding box"));
    }

    /// <inheritdoc/>
    public InteropBoundingBox Interop => _interop ?? throw new ObjectDisposedException(nameof(OwnedBoundingBox));

    /// <summary>The native handle, or zero once disposed.</summary>
    public nint Handle => _interop?.Handle ?? 0;

    /// <summary>The side length; the shim allocates it, so dispose it.</summary>
    public OwnedBlockPos? SideLength
    {
        get
        {
            var value = Interop.getSideLength();
            return value is null ? null : OwnedBlockPos.Adopt(value);
        }
    }

    /// <summary>Whether the box contains a point.</summary>
    public bool Contains(BlockPos point)
    {
        ArgumentNullException.ThrowIfNull(point);
        return Interop.contains(point.Interop);
    }

    /// <summary>Whether the box contains another box.</summary>
    public bool Contains(BoundingBox other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Interop.contains(other.Interop);
    }

    /// <summary>Merges a point in and returns the result.</summary>
    public BoundingBox? Merge(BlockPos point)
    {
        ArgumentNullException.ThrowIfNull(point);
        return Wrap(Interop.merge(point.Interop));
    }

    /// <summary>Merges another box in and returns the result.</summary>
    public BoundingBox? Merge(BoundingBox other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Wrap(Interop.merge(other.Interop));
    }

    /// <summary>Converts to a floating-point box, which the shim allocates.</summary>
    public OwnedAabb? ToAabb()
    {
        var value = Interop.operator_AABB();
        return value is null ? null : OwnedAabb.Adopt(value);
    }

    private static BoundingBox? Wrap(InteropBoundingBox? value) => value is null ? null : BoundingBox.From(value);

    /// <summary>Destroys the native box.</summary>
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
