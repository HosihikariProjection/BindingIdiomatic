using InteropPlayerChatEvent = Hosihikari.Bindings.Endstone.Interop.endstone.PlayerChatEvent;

namespace Hosihikari.Binding.Endstone;

/// <summary>A player sent a chat message.</summary>
public sealed class PlayerChatEvent : IInteropBridge<InteropPlayerChatEvent>
{
    private InteropPlayerChatEvent? _interop;
    private readonly BindingScope _scope;

    private PlayerChatEvent(InteropPlayerChatEvent interop, BindingScope scope)
    {
        _interop = interop;
        _scope = scope;
    }

    /// <summary>Bridges an Interop wrapper.</summary>
    public static PlayerChatEvent From(InteropPlayerChatEvent interop, BindingScope scope)
    {
        ArgumentNullException.ThrowIfNull(interop);
        return new PlayerChatEvent(interop, BindingScope.Require(scope));
    }

    /// <inheritdoc/>
    public InteropPlayerChatEvent Interop =>
        _interop ?? throw new ObjectDisposedException(nameof(PlayerChatEvent));

    /// <summary>The native handle, or zero once the instance was invalidated.</summary>
    public nint Handle => _interop?.Handle ?? 0;

    /// <summary>The player that sent the message.</summary>
    public Player? Player =>
        Interop.AsPlayerEvent()?.getPlayer() is { } player ? Player.From(player, _scope) : null;

    /// <summary>The message; the carrier writes a replacement back to the sender's text.</summary>
    public string Message
    {
        get => Interop.getMessage();
        set => Interop.setMessage(value);
    }

    /// <summary>Whether the event was cancelled.</summary>
    public bool IsCancelled => Interop.AsICancellable()?.isCancelled() ?? false;

    /// <summary>Cancels the message.</summary>
    public void Cancel() => Interop.AsICancellable()?.setCancelled(true);

    /// <summary>Invalidates this instance. The native event is untouched.</summary>
    public void Dispose() => _interop = null;
}
