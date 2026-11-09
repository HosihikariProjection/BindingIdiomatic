namespace Hosihikari.Binding.Endstone;

/// <summary>A borrowed command map.</summary>
/// <remarks>
/// The borrowed half: maps handed out by the server are borrowed. To make one, use
/// <see cref="OwnedCommandMap"/>. Lookups are raw shared handles until command wrappers exist.
/// </remarks>
public sealed class CommandMap : IInteropBridge<InteropCommandMap>
{
    private InteropCommandMap? _interop;

    private CommandMap(InteropCommandMap interop) => _interop = interop;

    /// <summary>Bridges an Interop wrapper.</summary>
    public static CommandMap From(InteropCommandMap interop)
    {
        ArgumentNullException.ThrowIfNull(interop);
        return new CommandMap(interop);
    }

    /// <inheritdoc/>
    public InteropCommandMap Interop => _interop ?? throw new ObjectDisposedException(nameof(CommandMap));

    /// <summary>The native handle, or zero once the instance was invalidated.</summary>
    public nint Handle => _interop?.Handle ?? 0;

    /// <summary>Looks a command up by name.</summary>
    /// <param name="name">The command name.</param>
    /// <remarks>Returns the raw shared handle; command wrappers are a later batch.</remarks>
    public Hosihikari.Bindings.Abi.SharedHandle? this[string name] => Interop.getCommand(name);

    /// <summary>Whether two maps are equal.</summary>
    public static bool operator ==(CommandMap? left, CommandMap? right) =>
        left is null ? right is null : right is not null && left.Handle == right.Handle;

    /// <summary>Whether two maps differ.</summary>
    public static bool operator !=(CommandMap? left, CommandMap? right) => !(left == right);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is CommandMap other && this == other;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Handle);

    /// <summary>Invalidates this instance. The native map is untouched.</summary>
    public void Dispose() => _interop = null;
}
