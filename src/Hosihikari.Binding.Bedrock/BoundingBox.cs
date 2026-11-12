namespace Hosihikari.Binding.Bedrock;

/// <summary>A borrowed block-aligned bounding box.</summary>
/// <remarks>
/// The borrowed half. To make one, use <see cref="OwnedBoundingBox"/>; to keep one, copy it
/// with <see cref="OwnedBoundingBox.CopyOf"/>.
/// </remarks>
public sealed class BoundingBox : IInteropBridge<InteropBoundingBox>
{
    private InteropBoundingBox? _interop;

    private BoundingBox(InteropBoundingBox interop) => _interop = interop;

    /// <summary>Bridges an Interop wrapper.</summary>
    public static BoundingBox From(InteropBoundingBox interop)
    {
        ArgumentNullException.ThrowIfNull(interop);
        return new BoundingBox(interop);
    }

    /// <inheritdoc/>
    public InteropBoundingBox Interop => _interop ?? throw new ObjectDisposedException(nameof(BoundingBox));

    /// <summary>The native handle, or zero once the instance was invalidated.</summary>
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

    private static BoundingBox? Wrap(InteropBoundingBox? value) => value is null ? null : From(value);

    /// <summary>Invalidates this instance. The native box is untouched.</summary>
    public void Dispose() => _interop = null;
}
