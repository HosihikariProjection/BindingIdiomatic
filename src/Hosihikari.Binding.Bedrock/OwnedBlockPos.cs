using InteropBlockPos = Hosihikari.Bindings.Bedrock.Interop.BlockPos;

namespace Hosihikari.Binding.Bedrock;

/// <summary>A block position the caller owns and must dispose.</summary>
/// <remarks>
/// The owned half of the block-position pair. The projected surface has no factory for a
/// block position, so one is only ever adopted from an accessor that allocated it; there is
/// no constructor and no <c>CopyOf</c>.
/// </remarks>
public sealed class OwnedBlockPos :
    IDisposable,
    IInteropBridge<InteropBlockPos>
{
    private InteropBlockPos? _interop;

    private OwnedBlockPos(InteropBlockPos interop) => _interop = interop;

    /// <summary>Adopts a wrapper the shim already allocated.</summary>
    internal static OwnedBlockPos Adopt(InteropBlockPos interop) => new(interop);

    /// <inheritdoc/>
    public InteropBlockPos Interop => _interop ?? throw new ObjectDisposedException(nameof(OwnedBlockPos));

    /// <summary>The native handle, or zero once disposed.</summary>
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

    /// <inheritdoc/>
    public override string ToString() => Interop.toString();

    /// <summary>Destroys the native block position.</summary>
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
