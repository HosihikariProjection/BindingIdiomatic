using System.Runtime.InteropServices;
using InteropEvent = global::Hosihikari.Binding.Endstone.Interop.endstone.Event;

namespace Hosihikari.Binding.Endstone;

/// <summary>A registration that dispatches a carrier event to a managed handler.</summary>
/// <remarks>
/// There is no per-subscription unregister call. The carrier removes every handler belonging to
/// a plugin when that plugin is disabled (<c>EndstonePluginManager::disablePlugin</c> calls
/// <c>HandlerList::unregister(plugin)</c>), which destroys the native handler, runs the release
/// pointer and frees the callback state. Disposing here only drops the managed delegate first —
/// until the carrier gets there, the trampoline keeps being invoked and returns immediately.
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
