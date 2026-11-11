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
        return new OwnedAabb(Snapshots.Rebuild(other.Interop));
    }

    /// <inheritdoc/>
    public InteropAabbB Interop => _interop ?? throw new ObjectDisposedException(nameof(OwnedAabb));

    /// <summary>The native handle, or zero once disposed.</summary>
    public nint Handle => _interop?.Handle ?? 0;

    /// <summary>The box centre.</summary>
    public Vec3 Center => Vec3.From(Interop.center());

    /// <summary>Grows the box by a distance and returns the result.</summary>
    public AABB Grow(Vec3 distance) => AABB.From(Interop.cloneAndGrow(distance.Interop));

    /// <summary>Grows the box by a radius and returns the result.</summary>
    public AABB Grow(float radius) => AABB.From(Interop.cloneAndGrow(radius));

    /// <summary>Shrinks the box by an offset and returns the result.</summary>
    public AABB Shrink(Vec3 offset) => AABB.From(Interop.shrink(offset.Interop));

    /// <summary>Shrinks the box by a scalar and returns the result.</summary>
    public AABB Shrink(float amount) => AABB.From(Interop.shrink(amount));

    /// <summary>Merges another box in and returns the result.</summary>
    public AABB Merge(AABB other) => AABB.From(Interop.merge(other.Interop));

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
