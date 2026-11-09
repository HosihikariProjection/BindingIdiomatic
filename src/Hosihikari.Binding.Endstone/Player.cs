using InteropPlayer = Hosihikari.Bindings.Endstone.Interop.endstone.Player;
using Hosihikari.Bindings.Endstone.Interop.endstone;

namespace Hosihikari.Binding.Endstone;

/// <summary>A player connected to the server.</summary>
/// <remarks>
/// Players are reached by borrowing, never created directly, so an instance obtained from an
/// event or <c>Server</c> lookup is only valid while the server keeps the player alive.
/// The type still implements <see cref="IDisposable"/> because <see cref="From"/> can absorb an
/// owned wrapper; disposing a borrowed instance only invalidates that instance.
/// </remarks>
public sealed class Player :
    IOwnedHandle,
    IInteropBridge<InteropPlayer>
{
    private InteropPlayer? _interop;

    private Player(InteropPlayer interop) => _interop = interop;

    /// <summary>Bridges an Interop wrapper, reusing the instance already registered for the target.</summary>
    /// <param name="interop">The wrapper to bridge.</param>
    /// <param name="scope">The scope owning the identity registry.</param>
    /// <exception cref="ArgumentNullException"><paramref name="interop"/> or <paramref name="scope"/> is null.</exception>
    public static Player From(InteropPlayer interop, BindingScope scope)
    {
        ArgumentNullException.ThrowIfNull(interop);
        var identities = BindingScope.Require(scope).Identities
            ?? throw new InvalidOperationException("The scope does not track identities.");

        var generation = scope.Generations?.GenerationOf(interop.Handle) ?? 0;
        var existing = identities.TryGet<Player>(interop.Handle, generation);
        if (existing is not null)
        {
            existing.Absorb(interop);
            return existing;
        }

        var created = new Player(interop);
        identities.GetOrAdd(interop.Handle, generation, () => created);
        return created;
    }

    /// <inheritdoc/>
    public InteropPlayer Interop =>
        _interop ?? throw new ObjectDisposedException(nameof(Player));

    /// <inheritdoc/>
    public nint Handle => _interop?.Handle ?? 0;

    /// <inheritdoc/>
    /// <remarks>Always <see langword="false"/> unless an owned wrapper was absorbed.</remarks>
    public bool OwnsHandle => _interop?.OwnsHandle ?? false;

    /// <summary>The player's name.</summary>
    public string Name => Interop.getName();

    /// <summary>The player's XUID.</summary>
    public string Xuid => Interop.getXuid();

    /// <summary>The player's locale.</summary>
    public string Locale => Interop.getLocale();

    /// <summary>The device operating system reported by the client.</summary>
    public string DeviceOs => Interop.getDeviceOS();

    /// <summary>The device identifier reported by the client.</summary>
    public string DeviceId => Interop.getDeviceId();

    /// <summary>The client's game version.</summary>
    public string GameVersion => Interop.getGameVersion();

    /// <summary>The player's current ping in milliseconds.</summary>
    public long Ping => Interop.getPing();

    /// <summary>The player's experience level.</summary>
    public int ExperienceLevel => Interop.getExpLevel();

    /// <summary>The player's experience progress toward the next level.</summary>
    public float ExperienceProgress => Interop.getExpProgress();

    /// <summary>The player's total experience.</summary>
    public int TotalExperience => Interop.getTotalExp();

    /// <summary>The player's walk speed.</summary>
    public float WalkSpeed => Interop.getWalkSpeed();

    /// <summary>The player's fly speed.</summary>
    public float FlySpeed => Interop.getFlySpeed();

    /// <summary>The player's game mode.</summary>
    public GameMode GameMode => (GameMode)Interop.getGameMode();

    /// <summary>The player's address.</summary>
    public SocketAddress Address => Interop.getAddress();

    /// <summary>The player's inventory.</summary>
    public PlayerInventory Inventory => Interop.getInventory();

    /// <summary>The player's ender chest inventory.</summary>
    public Inventory EnderChest => Interop.getEnderChest();

    /// <summary>The player's scoreboard.</summary>
    public Scoreboard Scoreboard => Interop.getScoreboard();

    /// <summary>The player's skin.</summary>
    public Skin Skin => Interop.getSkin();

    /// <summary>Whether the player may fly.</summary>
    public bool AllowFlight => Interop.getAllowFlight();

    /// <summary>Whether the player is currently flying.</summary>
    public bool IsFlying => Interop.isFlying();

    /// <summary>Whether the player is an operator.</summary>
    public bool IsOp => Interop.isOp();

    /// <summary>Whether the player is sneaking.</summary>
    public bool IsSneaking => Interop.isSneaking();

    /// <summary>Whether the player is sprinting.</summary>
    public bool IsSprinting => Interop.isSprinting();

    /// <summary>Closes the form currently open for the player.</summary>
    public void CloseForm() => Interop.closeForm();

    /// <summary>Gives the player experience.</summary>
    /// <param name="amount">The amount to give.</param>
    public void GiveExperience(int amount) => Interop.giveExp(amount);

    /// <summary>Gives the player experience levels.</summary>
    /// <param name="amount">The number of levels to give.</param>
    public void GiveExperienceLevels(int amount) => Interop.giveExpLevels(amount);

    /// <summary>Kicks the player with a message.</summary>
    /// <param name="message">The message shown to the player.</param>
    public void Kick(string message) => Interop.kick(message);

    /// <summary>Runs a command as the player.</summary>
    /// <param name="command">The command line.</param>
    /// <returns>Whether the command succeeded.</returns>
    public bool PerformCommand(string command) => Interop.performCommand(command);

    /// <summary>Plays a sound for the player.</summary>
    public void PlaySound(Location location, string sound, float volume, float pitch) =>
        Interop.playSound(location, sound, volume, pitch);

    /// <summary>Resets the player's title.</summary>
    public void ResetTitle() => Interop.resetTitle();

    /// <summary>Sends a form to the player.</summary>
    public void SendForm(Bindings.Endstone.Interop.Variant_e1e3d57e form) => Interop.sendForm(form);

    /// <summary>Sends a map to the player.</summary>
    public void SendMap(MapView map) => Interop.sendMap(map);

    /// <summary>Sends a raw packet to the player.</summary>
    public void SendPacket(int packetId, string payload) => Interop.sendPacket(packet_id: packetId, payload);

    /// <summary>Sends a popup to the player.</summary>
    public void SendPopup(string message) => Interop.sendPopup(message);

    /// <summary>Sends a tip to the player.</summary>
    public void SendTip(string message) => Interop.sendTip(message);

    /// <summary>Sends a title to the player.</summary>
    public void SendTitle(string title, string subtitle) => Interop.sendTitle(title, subtitle);

    /// <summary>Sends a title to the player with explicit fade timings.</summary>
    public void SendTitle(string title, string subtitle, int fadeIn, int stay, int fadeOut) =>
        Interop.sendTitle(title, subtitle, fade_in: fadeIn, stay, fade_out: fadeOut);

    /// <summary>Sends a toast to the player.</summary>
    public void SendToast(string title, string content) => Interop.sendToast(title, content);

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">The instance only borrows its handle.</exception>
    public nint ReleaseHandle()
    {
        var handle = Interop.ReleaseHandle();
        _interop = null;
        return handle;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Host-owned Endstone objects get no disposer from the shim, so disposing a borrowed
    /// instance only invalidates it. An absorbed owned wrapper is released.
    /// </remarks>
    public void Dispose()
    {
        var interop = _interop;
        if (interop is null)
        {
            return;
        }

        _interop = null;
        if (interop.OwnsHandle)
        {
            interop.ReleaseHandle();
        }
        else
        {
            interop.DisposeIfDisposable();
        }
    }

    private void Absorb(InteropPlayer incoming)
    {
        if (ReferenceEquals(incoming, _interop))
        {
            return;
        }

        // Only an owned incoming handle is worth transferring; a borrowed one is just dropped.
        if (incoming.OwnsHandle)
        {
            var transferred = incoming.ReleaseHandle();
            _interop?.DisposeIfDisposable();
            _interop = new InteropPlayer(transferred);
        }
        else
        {
            incoming.DisposeIfDisposable();
        }
    }
}
