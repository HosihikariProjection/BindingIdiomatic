using InteropPlayerJoinEvent = Hosihikari.Bindings.Endstone.Interop.endstone.PlayerJoinEvent;

namespace Hosihikari.Binding.Endstone;

/// <summary>A player joined the server.</summary>
public sealed class PlayerJoinEvent : IInteropBridge<InteropPlayerJoinEvent>
{
    private InteropPlayerJoinEvent? _interop;
    private readonly BindingScope _scope;

    private PlayerJoinEvent(InteropPlayerJoinEvent interop, BindingScope scope)
    {
        _interop = interop;
        _scope = scope;
    }

    /// <summary>Bridges an Interop wrapper.</summary>
    public static PlayerJoinEvent From(InteropPlayerJoinEvent interop, BindingScope scope)
    {
        ArgumentNullException.ThrowIfNull(interop);
        return new PlayerJoinEvent(interop, BindingScope.Require(scope));
    }

    /// <inheritdoc/>
    public InteropPlayerJoinEvent Interop =>
        _interop ?? throw new ObjectDisposedException(nameof(PlayerJoinEvent));

    /// <summary>The native handle, or zero once the instance was invalidated.</summary>
    public nint Handle => _interop?.Handle ?? 0;

    /// <summary>The player that joined.</summary>
    public Player? Player =>
        Interop.AsPlayerEvent()?.getPlayer() is { } player ? Player.From(player, _scope) : null;

    /// <summary>Invalidates this instance. The native event is untouched.</summary>
    public void Dispose() => _interop = null;
}
