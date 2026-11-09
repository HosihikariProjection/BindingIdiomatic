using InteropVec3 = Hosihikari.Bindings.Bedrock.Interop.Vec3;

namespace Hosihikari.Binding.Bedrock;

/// <summary>A three-component float vector.</summary>
/// <remarks>
/// Reached by borrowing only: the shim emits no factory for it.
/// The symbol-dispatch surface exposes the components but not the arithmetic, so the operators
/// stay on the Interop wrapper until that batch covers them.
/// </remarks>
public sealed class Vec3 : IInteropBridge<InteropVec3>
{
    private InteropVec3? _interop;

    private Vec3(InteropVec3 interop) => _interop = interop;

    /// <summary>Bridges an Interop wrapper.</summary>
    public static Vec3 From(InteropVec3 interop)
    {
        ArgumentNullException.ThrowIfNull(interop);
        return new Vec3(interop);
    }

    /// <inheritdoc/>
    public InteropVec3 Interop => _interop ?? throw new ObjectDisposedException(nameof(Vec3));

    /// <summary>The native handle, or zero once the instance was invalidated.</summary>
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

    /// <summary>Invalidates this instance. The native vector is untouched.</summary>
    public void Dispose() => _interop = null;

    /// <inheritdoc/>
    public override string ToString() => Interop.toString();
}
