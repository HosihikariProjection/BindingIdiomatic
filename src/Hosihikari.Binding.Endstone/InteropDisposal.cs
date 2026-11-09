namespace Hosihikari.Binding.Endstone;

/// <summary>Interop helpers that depend on whether the generated wrapper is disposable.</summary>
/// <remarks>
/// A wrapper for a host-owned object gets no disposer, so the generated type does not implement
/// <see cref="IDisposable"/>. Others do. Both shapes reach this layer, so the call has to be
/// conditional instead of a plain interface dispatch.
/// </remarks>
internal static class InteropDisposal
{
    /// <summary>Disposes the wrapper when the generated type implements <see cref="IDisposable"/>.</summary>
    public static void DisposeIfDisposable<T>(this T wrapper) where T : class
    {
        if (wrapper is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
