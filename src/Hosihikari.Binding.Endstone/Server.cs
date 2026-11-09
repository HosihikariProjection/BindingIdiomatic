namespace Hosihikari.Binding.Endstone;

/// <summary>The running server.</summary>
/// <remarks>
/// The shim generates no disposer for a host-owned Endstone object, and <c>Server.Create</c> is a
/// stub, so an instance is always borrowed and the host decides its lifetime. The type still
/// implements <see cref="IDisposable"/> so an owned wrapper can be absorbed.
/// </remarks>
public sealed class Server :
    IOwnedHandle,
    IInteropBridge<InteropServer>
{
    private InteropServer? _interop;

    private Server(InteropServer interop) => _interop = interop;

    /// <summary>Bridges an Interop wrapper, reusing the instance already registered for the server.</summary>
    /// <param name="interop">The wrapper to bridge.</param>
    /// <param name="scope">The scope owning the identity registry.</param>
    public static Server From(InteropServer interop, BindingScope scope)
    {
        ArgumentNullException.ThrowIfNull(interop);
        var identities = BindingScope.Require(scope).Identities
            ?? throw new InvalidOperationException("The scope does not track identities.");

        var generation = scope.Generations?.GenerationOf(interop.Handle) ?? 0;
        var existing = identities.TryGet<Server>(interop.Handle, generation);
        if (existing is not null)
        {
            existing.Absorb(interop);
            return existing;
        }

        var created = new Server(interop);
        identities.GetOrAdd(interop.Handle, generation, () => created);
        return created;
    }

    /// <inheritdoc/>
    public InteropServer Interop => _interop ?? throw new ObjectDisposedException(nameof(Server));

    /// <inheritdoc/>
    public nint Handle => _interop?.Handle ?? 0;

    /// <inheritdoc/>
    public bool OwnsHandle => _interop?.OwnsHandle ?? false;

    /// <summary>The server name.</summary>
    public string Name => Interop.getName();

    /// <summary>The server version.</summary>
    public string Version => Interop.getVersion();

    /// <summary>The Minecraft version.</summary>
    public string MinecraftVersion => Interop.getMinecraftVersion();

    /// <summary>The protocol version.</summary>
    public int ProtocolVersion => Interop.getProtocolVersion();

    /// <summary>The IPv4 port.</summary>
    public int Port => Interop.getPort();

    /// <summary>The IPv6 port.</summary>
    public int PortV6 => Interop.getPortV6();

    /// <summary>The maximum player count.</summary>
    public int MaxPlayers => Interop.getMaxPlayers();

    /// <summary>Whether online mode is required.</summary>
    public bool OnlineMode => Interop.getOnlineMode();

    /// <summary>Whether the current thread is the primary thread.</summary>
    public bool IsPrimaryThread => Interop.isPrimaryThread();

    /// <summary>The server start time.</summary>
    public long StartTime => Interop.getStartTime();

    /// <summary>Average ticks per second.</summary>
    public float AverageTicksPerSecond => Interop.getAverageTicksPerSecond();

    /// <summary>Average milliseconds per tick.</summary>
    public float AverageMillisecondsPerTick => Interop.getAverageMillisecondsPerTick();

    /// <summary>Average tick usage.</summary>
    public float AverageTickUsage => Interop.getAverageTickUsage();

    /// <summary>Current ticks per second.</summary>
    public float CurrentTicksPerSecond => Interop.getCurrentTicksPerSecond();

    /// <summary>Current milliseconds per tick.</summary>
    public float CurrentMillisecondsPerTick => Interop.getCurrentMillisecondsPerTick();

    /// <summary>Current tick usage.</summary>
    public float CurrentTickUsage => Interop.getCurrentTickUsage();

    /// <summary>The connected players.</summary>
    /// <remarks>Rebuilt per read; the elements are borrowed views, not copies.</remarks>
    public IReadOnlyList<Player> OnlinePlayers
    {
        get
        {
            var handles = Interop.getOnlinePlayers();
            if (handles.Length == 0)
            {
                return Array.Empty<Player>();
            }

            var scope = BindingScope.For(typeof(Server).Assembly);
            var players = new Player[handles.Length];
            for (var index = 0; index < handles.Length; index++)
            {
                players[index] = Player.From(InteropPlayer.Borrow(handles[index]), scope);
            }

            return players;
        }
    }

    /// <summary>The ban list.</summary>
    public PlayerBanList BanList => Interop.getBanList();

    /// <summary>The IP ban list.</summary>
    public IpBanList IpBanList => Interop.getIpBanList();

    /// <summary>The command sender for the console.</summary>
    public ConsoleCommandSender CommandSender => Interop.getCommandSender();

    /// <summary>The item factory.</summary>
    public ItemFactory ItemFactory => Interop.getItemFactory();

    /// <summary>The language.</summary>
    public Language Language => Interop.getLanguage();

    /// <summary>The welcome message logged at startup.</summary>
    public string Level => Interop.getLevel()?.getName() ?? "";

    /// <summary>The logger.</summary>
    public Logger Logger => Interop.getLogger();

    /// <summary>The plugin manager.</summary>
    public PluginManager PluginManager => Interop.getPluginManager();

    /// <summary>The scheduler.</summary>
    public Scheduler Scheduler => Interop.getScheduler();

    /// <summary>The service manager.</summary>
    public ServiceManager ServiceManager => Interop.getServiceManager();

    /// <summary>Looks a player up by name.</summary>
    /// <param name="name">The player's name.</param>
    public Player? this[string name]
    {
        get
        {
            var wrapper = Interop.getPlayer(name);
            return wrapper is null ? null : Player.From(wrapper, BindingScope.For(typeof(Server).Assembly));
        }
    }

    /// <summary>Looks a player up by UUID.</summary>
    /// <param name="id">The player's UUID.</param>
    public Player? this[UUID id]
    {
        get
        {
            var wrapper = Interop.getPlayer(id);
            return wrapper is null ? null : Player.From(wrapper, BindingScope.For(typeof(Server).Assembly));
        }
    }

    /// <summary>Broadcasts a raw message.</summary>
    public void Broadcast(Bindings.Endstone.Interop.Variant_228e24a8 message) => Interop.broadcastMessage(message);

    /// <summary>Broadcasts a message to players with a permission.</summary>
    public void Broadcast(Bindings.Endstone.Interop.Variant_228e24a8 message, string permission) =>
        Interop.broadcast(message, permission);

    /// <summary>Runs a command as a sender.</summary>
    /// <returns>Whether the command succeeded.</returns>
    public bool DispatchCommand(CommandSender sender, string commandLine) =>
        Interop.dispatchCommand(sender, commandLine);

    /// <summary>Reloads the server.</summary>
    public void Reload() => Interop.reload();

    /// <summary>Reloads server data.</summary>
    public void ReloadData() => Interop.reloadData();

    /// <summary>Sets the maximum player count.</summary>
    public void SetMaxPlayers(int maxPlayers) => Interop.setMaxPlayers(max_players: maxPlayers);

    /// <summary>Shuts the server down.</summary>
    public void Shutdown() => Interop.shutdown();

    /// <inheritdoc/>
    public nint ReleaseHandle()
    {
        var handle = Interop.ReleaseHandle();
        _interop = null;
        return handle;
    }

    /// <inheritdoc/>
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

    private void Absorb(InteropServer incoming)
    {
        if (ReferenceEquals(incoming, _interop))
        {
            return;
        }

        if (incoming.OwnsHandle)
        {
            var transferred = incoming.ReleaseHandle();
            _interop?.DisposeIfDisposable();
            _interop = new InteropServer(transferred);
        }
        else
        {
            incoming.DisposeIfDisposable();
        }
    }
}
