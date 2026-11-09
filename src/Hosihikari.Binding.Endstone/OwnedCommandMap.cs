namespace Hosihikari.Binding.Endstone;

/// <summary>A command map the caller owns and must dispose.</summary>
/// <remarks>
/// The owned half of the command-map pair. The native map is destroyed on dispose.
/// </remarks>
public sealed class OwnedCommandMap :
    IDisposable,
    IInteropBridge<InteropCommandMap>
{
    private InteropCommandMap? _interop;

    private OwnedCommandMap(InteropCommandMap interop) => _interop = interop;

    /// <summary>Creates an empty command map.</summary>
    public OwnedCommandMap() : this(InteropCommandMap.Create()) { }

    /// <summary>Copies a borrowed map into an owned one.</summary>
    public static OwnedCommandMap CopyOf(CommandMap other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new OwnedCommandMap(InteropCommandMap.Create(other.Handle));
    }

    /// <inheritdoc/>
    public InteropCommandMap Interop => _interop ?? throw new ObjectDisposedException(nameof(OwnedCommandMap));

    /// <summary>The native handle, or zero once disposed.</summary>
    public nint Handle => _interop?.Handle ?? 0;

    /// <summary>Looks a command up by name.</summary>
    public Hosihikari.Bindings.Abi.SharedHandle? this[string name] => Interop.getCommand(name);

    /// <summary>Registers a command.</summary>
    /// <param name="command">The command handle.</param>
    /// <returns>Whether registration succeeded.</returns>
    public bool RegisterCommand(Hosihikari.Bindings.Abi.SharedHandle? command) =>
        Interop.registerCommand(command);

    /// <summary>Runs a command line as a sender.</summary>
    /// <returns>Whether the command succeeded.</returns>
    public bool Dispatch(CommandSender sender, string commandLine)
    {
        ArgumentNullException.ThrowIfNull(sender);
        return Interop.dispatch(sender, commandLine);
    }

    /// <summary>Removes every registered command.</summary>
    public void ClearCommands() => Interop.clearCommands();

    /// <summary>Replaces the contents with another map.</summary>
    public void AssignFrom(CommandMap other)
    {
        ArgumentNullException.ThrowIfNull(other);
        Interop.operator_assign(other.Interop);
    }

    /// <summary>Destroys the native map.</summary>
    public void Dispose()
    {
        var interop = _interop;
        if (interop is null)
        {
            return;
        }

        _interop = null;
        interop.DisposeIfDisposable();
    }
}
