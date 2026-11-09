using InteropCommandRegistrar = Hosihikari.Bindings.LeviLamina.Interop.ll.command.CommandRegistrar;

namespace Hosihikari.Binding.LeviLamina;

/// <summary>The loader's command registry.</summary>
/// <remarks>
/// Borrowed only: the registrar is a loader singleton. Schema names follow the native ones; enum
/// values travel as key/value pairs with the underlying value type, as the shim expects.
/// </remarks>
public sealed class CommandRegistrar : IInteropBridge<InteropCommandRegistrar>
{
    private InteropCommandRegistrar? _interop;

    private CommandRegistrar(InteropCommandRegistrar interop) => _interop = interop;

    /// <summary>The server-side registrar.</summary>
    public static CommandRegistrar ServerInstance => From(InteropCommandRegistrar.getServerInstance());

    /// <summary>The registrar for a side.</summary>
    /// <param name="isClientSide">Whether the client-side registrar is wanted.</param>
    public static CommandRegistrar Instance(bool isClientSide) =>
        From(InteropCommandRegistrar.getInstance(isClientSide));

    /// <summary>Bridges an Interop wrapper.</summary>
    public static CommandRegistrar From(InteropCommandRegistrar interop)
    {
        ArgumentNullException.ThrowIfNull(interop);
        return new CommandRegistrar(interop);
    }

    /// <inheritdoc/>
    public InteropCommandRegistrar Interop =>
        _interop ?? throw new ObjectDisposedException(nameof(CommandRegistrar));

    /// <summary>The native handle, or zero once the instance was invalidated.</summary>
    public nint Handle => _interop?.Handle ?? 0;

    /// <summary>Whether an enum is registered.</summary>
    public bool HasEnum(string name) => Interop.hasEnum(name);

    /// <summary>Whether a soft enum is registered.</summary>
    public bool HasSoftEnum(string name) => Interop.hasSoftEnum(name);

    /// <summary>Registers an enum with a parser handle.</summary>
    public bool TryRegisterEnum(string name, KeyValuePair<string, ulong>[] values, ushort type, nint parser) =>
        Interop.tryRegisterEnum(name, values, type, parser);

    /// <summary>Registers a runtime enum.</summary>
    public bool TryRegisterRuntimeEnum(string name, KeyValuePair<string, ulong>[] values) =>
        Interop.tryRegisterRuntimeEnum(name, values);

    /// <summary>Registers a soft enum with its values.</summary>
    public bool TryRegisterSoftEnum(string name, string[] values) =>
        Interop.tryRegisterSoftEnum(name, values);

    /// <summary>Adds values to an existing enum.</summary>
    public bool AddEnumValues(string name, KeyValuePair<string, ulong>[] values, ushort type) =>
        Interop.addEnumValues(name, values, type);

    /// <summary>Adds values to a runtime enum.</summary>
    public bool AddRuntimeEnumValues(string name, KeyValuePair<string, ulong>[] values) =>
        Interop.addRuntimeEnumValues(name, values);

    /// <summary>Adds values to a soft enum.</summary>
    public bool AddSoftEnumValues(string name, string[] values) => Interop.addSoftEnumValues(name, values);

    /// <summary>Removes values from a soft enum.</summary>
    public bool RemoveSoftEnumValues(string name, string[] values) => Interop.removeSoftEnumValues(name, values);

    /// <summary>Replaces the values of a soft enum.</summary>
    public bool SetSoftEnumValues(string name, string[] values) => Interop.setSoftEnumValues(name, values);

    /// <summary>Creates or fetches a command by name.</summary>
    public Hosihikari.Bindings.LeviLamina.Interop.ll.command.CommandHandle GetOrCreateCommand(
        string name,
        string description,
        byte requirement,
        Hosihikari.Bindings.LeviLamina.Interop.CommandFlag flag,
        Hosihikari.Bindings.Abi.SharedHandle? mod) =>
        Interop.getOrCreateCommand(name, description, requirement, flag, mod);

    /// <summary>Invalidates this instance. The native registrar is untouched.</summary>
    public void Dispose() => _interop = null;
}
