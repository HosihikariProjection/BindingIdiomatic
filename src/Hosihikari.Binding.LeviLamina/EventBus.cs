using InteropEventBus = Hosihikari.Bindings.LeviLamina.Interop.ll.@event.EventBus;
using InteropEventIdView = Hosihikari.Bindings.LeviLamina.Interop.ll.@event.EventIdView;
using InteropSharedHandle = Hosihikari.Bindings.Abi.SharedHandle;

namespace Hosihikari.Binding.LeviLamina;

/// <summary>The loader's event bus.</summary>
/// <remarks>
/// Borrowed only: the bus is a loader singleton and nothing owns an instance. Listeners are raw
/// shared handles until a listener wrapper exists.
/// </remarks>
public sealed class EventBus : IInteropBridge<InteropEventBus>
{
    private InteropEventBus? _interop;

    private EventBus(InteropEventBus interop) => _interop = interop;

    /// <summary>The bus instance.</summary>
    public static EventBus Instance => From(InteropEventBus.getInstance());

    /// <summary>Bridges an Interop wrapper.</summary>
    public static EventBus From(InteropEventBus interop)
    {
        ArgumentNullException.ThrowIfNull(interop);
        return new EventBus(interop);
    }

    /// <inheritdoc/>
    public InteropEventBus Interop => _interop ?? throw new ObjectDisposedException(nameof(EventBus));

    /// <summary>The native handle, or zero once the instance was invalidated.</summary>
    public nint Handle => _interop?.Handle ?? 0;

    /// <summary>The event id's listener count.</summary>
    /// <param name="eventId">The event id.</param>
    public ulong this[InteropEventIdView eventId] => Interop.getListenerCount(eventId);

    /// <summary>The listener registered under an id, or <see langword="null"/>.</summary>
    /// <param name="id">The listener id.</param>
    public InteropSharedHandle? this[ulong id] => Interop.getListener(id);

    /// <summary>Whether an event id has listeners.</summary>
    public bool HasEvent(InteropEventIdView eventId) => Interop.hasEvent(eventId);

    /// <summary>Whether a listener is registered at all.</summary>
    public bool HasListener(InteropSharedHandle listener) => Interop.hasListener(listener);

    /// <summary>Whether a listener is registered under an id.</summary>
    public bool HasListener(ulong id) => Interop.hasListener(id);

    /// <summary>Whether a listener is registered for an event id.</summary>
    public bool HasListener(ulong id, InteropEventIdView eventId) => Interop.hasListener(id, eventId);

    /// <summary>Adds a listener for an event id.</summary>
    public bool AddListener(InteropSharedHandle listener, InteropEventIdView eventId) =>
        Interop.addListener(listener, eventId);

    /// <summary>Removes a listener.</summary>
    public bool RemoveListener(InteropSharedHandle listener) => Interop.removeListener(listener);

    /// <summary>Removes a listener from an event id.</summary>
    public bool RemoveListener(InteropSharedHandle listener, InteropEventIdView eventId) =>
        Interop.removeListener(listener, eventId);

    /// <summary>Removes a listener by id.</summary>
    public bool RemoveListener(ulong id) => Interop.removeListener(id);

    /// <summary>Publishes an event.</summary>
    public void Publish(
        Hosihikari.Bindings.LeviLamina.Interop.ll.@event.Event @event,
        InteropEventIdView eventId) => Interop.publish(@event, eventId);

    /// <summary>Publishes an event under a module name.</summary>
    public void Publish(
        string modName,
        Hosihikari.Bindings.LeviLamina.Interop.ll.@event.Event @event,
        InteropEventIdView eventId) => Interop.publish(modName, @event, eventId);

    /// <summary>Invalidates this instance. The native bus is untouched.</summary>
    public void Dispose() => _interop = null;
}
