using InteropEvent = Hosihikari.Bindings.Endstone.Interop.endstone.Event;

namespace Hosihikari.Binding.Endstone;

/// <summary>An event being dispatched to a handler.</summary>
/// <remarks>
/// A callback-scoped borrowed view: it is valid only for the handler that received it and is
/// not tracked by the identity registry.
/// </remarks>
public sealed class Event : IInteropBridge<InteropEvent>
{
    private InteropEvent? _interop;
    private readonly BindingScope _scope;

    private Event(InteropEvent interop, BindingScope scope)
    {
        _interop = interop;
        _scope = scope;
    }

    /// <summary>Bridges an Interop wrapper.</summary>
    public static Event From(InteropEvent interop, BindingScope scope)
    {
        ArgumentNullException.ThrowIfNull(interop);
        return new Event(interop, BindingScope.Require(scope));
    }

    /// <inheritdoc/>
    public InteropEvent Interop => _interop ?? throw new ObjectDisposedException(nameof(Event));

    /// <summary>The native handle, or zero once the instance was invalidated.</summary>
    public nint Handle => _interop?.Handle ?? 0;

    /// <summary>The carrier's event name.</summary>
    public string Name => Interop.getEventName();

    /// <summary>Whether the carrier dispatches the event asynchronously.</summary>
    public bool IsAsynchronous => Interop.isAsynchronous();

    /// <summary>The event as a player-join event, or <see langword="null"/> for another type.</summary>
    public PlayerJoinEvent? AsPlayerJoin() =>
        Interop.TryAsPlayerJoinEvent() is { } value ? PlayerJoinEvent.From(value, _scope) : null;

    /// <summary>The event as a player-quit event, or <see langword="null"/> for another type.</summary>
    public PlayerQuitEvent? AsPlayerQuit() =>
        Interop.TryAsPlayerQuitEvent() is { } value ? PlayerQuitEvent.From(value, _scope) : null;

    /// <summary>The event as a player-chat event, or <see langword="null"/> for another type.</summary>
    public PlayerChatEvent? AsPlayerChat() =>
        Interop.TryAsPlayerChatEvent() is { } value ? PlayerChatEvent.From(value, _scope) : null;

    /// <summary>Invalidates this instance. The native event is untouched.</summary>
    public void Dispose() => _interop = null;
}
