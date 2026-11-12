using System.Runtime.InteropServices;
using InteropEvent = Hosihikari.Bindings.Endstone.Interop.endstone.Event;

namespace Hosihikari.Binding.Endstone;

/// <summary>A registration that dispatches a carrier event to a managed handler.</summary>
/// <remarks>
/// The carrier has no unregister call: the native handler lives as long as the owner plugin.
/// Disposing therefore only drops the managed delegate — the native side keeps invoking the
/// trampoline, which returns immediately. The callback state stays rooted until the carrier
/// destroys the native handler and calls the release pointer.
/// </remarks>
public sealed class EventSubscription : IDisposable
{
    private readonly GCHandle _self;
    private readonly BindingScope _scope;
    private Action<Event>? _handler;

    internal EventSubscription(Action<Event> handler, BindingScope scope)
    {
        _handler = handler;
        _scope = scope;
        _self = GCHandle.Alloc(this);
    }

    internal nint UserData => GCHandle.ToIntPtr(_self);

    internal static unsafe nint InvokePointer =>
        (nint)(delegate* unmanaged<nint, nint, void>)&EventTrampoline.Invoke;

    internal static unsafe nint ReleasePointer =>
        (nint)(delegate* unmanaged<nint, void>)&EventTrampoline.Release;

    internal void Dispatch(nint eventHandle)
    {
        var handler = _handler;
        if (handler is null)
        {
            return;
        }

        try
        {
            EventFiring.Fire(_scope, handler, Event.From(InteropEvent.Borrow(eventHandle), _scope));
        }
        catch (Exception exception)
        {
            _scope.Report(new BindingError(handler, exception));
        }
    }

    /// <summary>Stops delivery to the handler. The native registration remains.</summary>
    public void Dispose() => Interlocked.Exchange(ref _handler, null);
}

internal static class EventTrampoline
{
    [UnmanagedCallersOnly]
    internal static void Invoke(nint userData, nint eventHandle)
    {
        if (GCHandle.FromIntPtr(userData).Target is EventSubscription subscription)
        {
            subscription.Dispatch(eventHandle);
        }
    }

    [UnmanagedCallersOnly]
    internal static void Release(nint userData)
    {
        var handle = GCHandle.FromIntPtr(userData);
        if (handle.IsAllocated)
        {
            handle.Free();
        }
    }
}
