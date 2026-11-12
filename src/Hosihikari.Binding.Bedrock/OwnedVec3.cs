namespace Hosihikari.Binding.Bedrock;

/// <summary>A three-component vector the caller owns and must dispose.</summary>
/// <remarks>
/// The owned half of the vector pair. Several accessors — the box and block-position centres —
/// have the shim allocate a vector, so those return this type rather than the borrowed
/// <see cref="Vec3"/>.
/// </remarks>
public sealed class OwnedVec3 :
    IDisposable,
    IInteropBridge<InteropVec3>
{
    private InteropVec3? _interop;

    private OwnedVec3(InteropVec3 interop) => _interop = interop;

    /// <summary>Adopts a wrapper the shim already allocated.</summary>
    internal static OwnedVec3 Adopt(InteropVec3 interop) => new(interop);

    /// <inheritdoc/>
    public InteropVec3 Interop => _interop ?? throw new ObjectDisposedException(nameof(OwnedVec3));

    /// <summary>The native handle, or zero once disposed.</summary>
    public nint Handle => _interop?.Handle ?? 0;

    /// <summary>The X component.</summary>
    public float X
    {
        get => Interop.x;
        set => Interop.x = value;
    }

    /// <summary>The Y component.</summary>
    public float Y
    {
        get => Interop.y;
        set => Interop.y = value;
    }

    /// <summary>The Z component.</summary>
    public float Z
    {
        get => Interop.z;
        set => Interop.z = value;
    }

    /// <summary>Destroys the native vector.</summary>
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
