using InteropServiceFunctions = Hosihikari.Bindings.LeviLamina.Interop.ll.service.Functions;

namespace Hosihikari.Binding.LeviLamina;

/// <summary>Service locator accessors.</summary>
/// <remarks>
/// Every accessor returns a raw handle: the pointed-to types (the level, the server instance, the
/// network system) are not part of the LLAPI surface, so there is no wrapper to hand back.
/// The handles are borrowed and owned by the loader.
/// </remarks>
public static class Services
{
    /// <summary>The dedicated server, or <see langword="null"/> when not available.</summary>
    public static nint? DedicatedServer => InteropServiceFunctions.getDedicatedServer();

    /// <summary>The level, or <see langword="null"/> when the loader has not published it.</summary>
    public static nint? Level => InteropServiceFunctions.getLevel();

    /// <summary>The server instance, or <see langword="null"/> when not available.</summary>
    public static nint? ServerInstance => InteropServiceFunctions.getServerInstance();

    /// <summary>The network system, or <see langword="null"/> when not available.</summary>
    public static nint? NetworkSystem(bool isClientSide) => InteropServiceFunctions.getNetworkSystem(isClientSide);

    /// <summary>The Minecraft instance, or <see langword="null"/> when not available.</summary>
    public static nint? Minecraft(bool isClientSide) => InteropServiceFunctions.getMinecraft(isClientSide);

    /// <summary>The command registry, or <see langword="null"/> when not available.</summary>
    public static nint? CommandRegistry(bool isClientSide) => InteropServiceFunctions.getCommandRegistry(isClientSide);

    /// <summary>The server network handler, or <see langword="null"/> when not available.</summary>
    public static nint? ServerNetworkHandler(bool isClientSide) => InteropServiceFunctions.getServerNetworkHandler(isClientSide);

    /// <summary>The resource pack repository, or <see langword="null"/> when not available.</summary>
    public static nint? ResourcePackRepository(bool isClientSide) => InteropServiceFunctions.getResourcePackRepository(isClientSide);

    /// <summary>The properties settings, or <see langword="null"/> when not available.</summary>
    public static nint? PropertiesSettings => InteropServiceFunctions.getPropertiesSettings();

    /// <summary>The RakNet peer, or <see langword="null"/> when not available.</summary>
    public static nint? RakPeer => InteropServiceFunctions.getRakPeer();

    /// <summary>The database storage, or <see langword="null"/> when not available.</summary>
    public static nint? DatabaseStorage => InteropServiceFunctions.getDBStorage();
}
