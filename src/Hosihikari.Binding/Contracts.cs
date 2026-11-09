namespace Hosihikari.Binding;

/// <summary>
/// A disposable object whose lifetime is owned by the caller and which can hand its handle out exactly once.
/// </summary>
/// <remarks>
/// A borrowed instance can still be disposed and can still be read through <c>Interop</c>,
/// but it cannot release its handle: <see cref="ReleaseHandle"/> throws for it.
/// </remarks>
public interface IOwnedHandle : IDisposable
{
    /// <summary>The native handle, or zero once the instance is disposed.</summary>
    nint Handle { get; }

    /// <summary>Whether this instance owns the handle rather than borrowing it.</summary>
    bool OwnsHandle { get; }

    /// <summary>Transfers ownership to the caller and invalidates this instance.</summary>
    /// <exception cref="ObjectDisposedException">The instance was already disposed or released.</exception>
    /// <exception cref="InvalidOperationException">The instance only borrows the handle.</exception>
    nint ReleaseHandle();
}

/// <summary>
/// Exposes the Interop wrapper an Idiomatic type wraps, so callers can reach anything not carried over.
/// </summary>
/// <remarks>
/// The property is the escape hatch. It returns the live wrapper, never a copy, so mutations made
/// through it are visible to the Idiomatic instance and the other way around.
/// </remarks>
public interface IInteropBridge<out TInterop>
{
    /// <summary>The wrapped Interop wrapper.</summary>
    /// <exception cref="ObjectDisposedException">This instance was disposed.</exception>
    TInterop Interop { get; }
}

/// <summary>
/// Supplies the generation used to validate identity registry entries for a native target.
/// </summary>
/// <remarks>
/// The projection has no uniform way to read a generation off a native object, so the surface that
/// owns the object supplies one. A surface without a generation source returns a constant; the
/// registry then degenerates to lifetime-only identity and stale entries survive until the weak
/// reference dies or the target is explicitly removed.
/// </remarks>
public interface IGenerationSource
{
    /// <summary>The current generation for <paramref name="target"/>.</summary>
    int GenerationOf(nint target);
}
