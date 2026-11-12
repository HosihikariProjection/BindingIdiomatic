using InteropPlayerQuitEvent = Hosihikari.Bindings.Endstone.Interop.endstone.PlayerQuitEvent;

namespace Hosihikari.Binding.Endstone;

/// <summary>A player quit the server.</summary>
/// <remarks>The capability layer normalises this to a player-leave event.</remarks>
public sealed class PlayerQuitEvent : IInteropBridge<InteropPlayerQuitEvent>
{
    private InteropPlayerQuitEvent? _interop;
    private readonly BindingScope _scope;

    private PlayerQuitEvent(InteropPlayerQuitEvent interop, BindingScope scope)
    {
        _interop = interop;
        _scope = scope;
    }

    /// <summary>Bridges an Interop wrapper.</summary>
    public static PlayerQuitEvent From(InteropPlayerQuitEvent interop, BindingScope scope)
    {
        ArgumentNullException.ThrowIfNull(interop);
        return new PlayerQuitEvent(interop, BindingScope.Require(scope));
    }

    /// <inheritdoc/>
    public InteropPlayerQuitEvent Interop =>
        _interop ?? throw new ObjectDisposedException(nameof(PlayerQuitEvent));

    /// <summary>The native handle, or zero once the instance was invalidated.</summary>
    public nint Handle => _interop?.Handle ?? 0;

    /// <summary>The player that quit.</summary>
    public Player? Player =>
        Interop.AsPlayerEvent()?.getPlayer() is { } player ? Player.From(player, _scope) : null;

    /// <summary>Invalidates this instance. The native event is untouched.</summary>
    public void Dispose() => _interop = null;
}
