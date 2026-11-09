using System.Reflection;
using System.Runtime.CompilerServices;
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

    /// <summary>The server-side registrar, registered on the caller's default scope.</summary>
    public static CommandRegistrar ServerInstance => From(InteropCommandRegistrar.getServerInstance(), CallerScope());

    /// <summary>The registrar for a side, registered on the caller's default scope.</summary>
    /// <param name="isClientSide">Whether the client-side registrar is wanted.</param>
    public static CommandRegistrar Instance(bool isClientSide) =>
        From(InteropCommandRegistrar.getInstance(isClientSide), CallerScope());

    /// <summary>The server-side registrar on an explicit scope.</summary>
    /// <param name="scope">The scope that owns the bridged identity.</param>
    public static CommandRegistrar ServerInstanceOn(BindingScope scope) =>
        From(InteropCommandRegistrar.getServerInstance(), scope);

    /// <summary>Bridges an Interop wrapper, reusing the instance already registered for the target.</summary>
    /// <param name="interop">The wrapper to bridge.</param>
    /// <param name="scope">The scope owning the identity registry.</param>
    public static CommandRegistrar From(InteropCommandRegistrar interop, BindingScope scope)
    {
        ArgumentNullException.ThrowIfNull(interop);
        var identities = BindingScope.Require(scope).Identities
            ?? throw new InvalidOperationException("The scope does not track identities.");

        var generation = scope.Generations?.GenerationOf(interop.Handle) ?? 0;
        var existing = identities.TryGet<CommandRegistrar>(interop.Handle, generation);
        if (existing is not null)
        {
            existing._interop = interop;
            return existing;
        }

        var created = new CommandRegistrar(interop);
        identities.GetOrAdd(interop.Handle, generation, () => created);
        return created;
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

    /// <summary>
    /// The caller's default scope. Kept out of line because Assembly.GetCallingAssembly is
    /// unreliable once the property is inlined.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static BindingScope CallerScope() => BindingScope.For(Assembly.GetCallingAssembly());

    /// <summary>Invalidates this instance. The native registrar is untouched.</summary>
    public void Dispose() => _interop = null;
}
