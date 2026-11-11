namespace Hosihikari.Binding.Bedrock;

/// <summary>An axis-aligned bounding box the caller owns and must dispose.</summary>
/// <remarks>
/// The owned half of the box pair. The native box is destroyed on dispose.
/// </remarks>
public sealed class OwnedAabb :
    IDisposable,
    IInteropBridge<InteropAabbB>
{
    private InteropAabbB? _interop;

    private OwnedAabb(InteropAabbB interop) => _interop = interop;

    /// <summary>Creates a box spanning two corners.</summary>
    public OwnedAabb(Vec3 min, Vec3 max)
    {
        ArgumentNullException.ThrowIfNull(min);
        ArgumentNullException.ThrowIfNull(max);
        _interop = Factories.Require(InteropAabbB.Create(min.Interop, max.Interop), "a bounding box");
    }

    /// <summary>Copies a borrowed box into an owned one.</summary>
    public static OwnedAabb CopyOf(AABB other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new OwnedAabb(Factories.Require(Snapshots.Rebuild(other.Interop), "a copied bounding box"));
    }

    /// <inheritdoc/>
    public InteropAabbB Interop => _interop ?? throw new ObjectDisposedException(nameof(OwnedAabb));

    /// <summary>The native handle, or zero once disposed.</summary>
    public nint Handle => _interop?.Handle ?? 0;

    /// <summary>The box centre, or <see langword="null"/> when the carrier does not export it.</summary>
    public Vec3? Center => Interop.center() is { } value ? Vec3.From(value) : null;

    /// <summary>Grows the box by a distance and returns the new box, which the shim allocates.</summary>
    public AABB? Grow(Vec3 distance)
    {
        ArgumentNullException.ThrowIfNull(distance);
        var value = Interop.cloneAndGrow(distance.Interop);
        return value is null ? null : AABB.From(value);
    }

    /// <summary>Grows the box by a radius and returns the new box, which the shim allocates.</summary>
    public AABB? Grow(float radius)
    {
        var value = Interop.cloneAndGrow(radius);
        return value is null ? null : AABB.From(value);
    }

    /// <summary>Shrinks the box by an offset and returns the result.</summary>
    public AABB? Shrink(Vec3 offset) => Wrap(Interop.shrink(offset.Interop));

    /// <summary>Shrinks the box by a scalar and returns the result.</summary>
    public AABB? Shrink(float amount) => Wrap(Interop.shrink(amount));

    /// <summary>Merges another box in and returns the result.</summary>
    public AABB? Merge(AABB other) => Wrap(Interop.merge(other.Interop));

    private static AABB? Wrap(InteropAabbB? value) => value is null ? null : AABB.From(value);

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
