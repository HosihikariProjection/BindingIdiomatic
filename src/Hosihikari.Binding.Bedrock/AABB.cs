using InteropAabb = Hosihikari.Bindings.Bedrock.Interop.AABB;

namespace Hosihikari.Binding.Bedrock;

/// <summary>A borrowed axis-aligned bounding box.</summary>
/// <remarks>
/// The borrowed half. To make one, use <see cref="OwnedAabb"/>; to keep one, copy it with
/// <see cref="OwnedAabb.CopyOf"/>.
/// </remarks>
public sealed class AABB : IInteropBridge<InteropAabb>
{
    private InteropAabb? _interop;

    private AABB(InteropAabb interop) => _interop = interop;

    /// <summary>Bridges an Interop wrapper.</summary>
    public static AABB From(InteropAabb interop)
    {
        ArgumentNullException.ThrowIfNull(interop);
        return new AABB(interop);
    }

    /// <inheritdoc/>
    public InteropAabb Interop => _interop ?? throw new ObjectDisposedException(nameof(AABB));

    /// <summary>The native handle, or zero once the instance was invalidated.</summary>
    public nint Handle => _interop?.Handle ?? 0;

    /// <summary>The box centre; the shim allocates it, so dispose it.</summary>
    public OwnedVec3? Center => Interop.center() is { } value ? OwnedVec3.Adopt(value) : null;

    /// <summary>Whether the box contains another box.</summary>
    public bool Contains(AABB other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Interop.contains(other.Interop);
    }

    /// <summary>Whether the box contains a point.</summary>
    public bool Contains(Vec3 point)
    {
        ArgumentNullException.ThrowIfNull(point);
        return Interop.contains(point.Interop);
    }

    /// <summary>Whether the box intersects another box.</summary>
    public bool Intersects(AABB other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Interop.intersects(other.Interop);
    }

    /// <summary>The distance from this box to another.</summary>
    public float DistanceTo(AABB other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Interop.distanceTo(other.Interop);
    }

    /// <summary>Invalidates this instance. The native box is untouched.</summary>
    public void Dispose() => _interop = null;
}
