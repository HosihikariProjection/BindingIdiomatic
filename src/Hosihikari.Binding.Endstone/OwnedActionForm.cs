using System.Runtime.InteropServices;
using InteropActionForm = Hosihikari.Bindings.Endstone.Interop.endstone.ActionForm;
using InteropNativeMethods = Hosihikari.Bindings.Endstone.Interop.NativeMethods;
using InteropVariantForm = Hosihikari.Bindings.Endstone.Interop.Variant_e1e3d57e;
using InteropVariantText = Hosihikari.Bindings.Endstone.Interop.Variant_228e24a8;

namespace Hosihikari.Binding.Endstone;

/// <summary>An action form the caller owns until it is shown.</summary>
/// <remarks>
/// The carrier copies the form when it is shown, so the caller disposes this instance right
/// after <see cref="Show"/>; the callbacks live on in the native copy.
/// </remarks>
public sealed class OwnedActionForm : IDisposable
{
    private InteropActionForm? _form;
    private readonly BindingScope _scope;

    /// <summary>Creates a form with a title and content.</summary>
    public OwnedActionForm(BindingScope scope, string title, string content)
    {
        _scope = BindingScope.Require(scope);
        _form = InteropActionForm.Create()
            ?? throw new InvalidOperationException(
                $"Could not create an action form: {InteropNativeMethods.GetLastError() ?? "the shim returned no object."}");
        SetTitle(title);
        SetContent(content);
    }

    private InteropActionForm Interop => _form ?? throw new ObjectDisposedException(nameof(OwnedActionForm));

    /// <summary>Sets the title.</summary>
    public OwnedActionForm SetTitle(string title)
    {
        var form = Interop.AsForm_endstone__ActionForm_()
            ?? throw new InvalidOperationException("The carrier did not expose the form base.");
        using var text = InteropVariantText.CreateString(title);
        form.setTitle(text);
        return this;
    }

    /// <summary>Sets the body text.</summary>
    public OwnedActionForm SetContent(string content)
    {
        using var text = InteropVariantText.CreateString(content);
        Interop.setContent(text);
        return this;
    }

    /// <summary>Adds a button; the handler runs when the player clicks it.</summary>
    public OwnedActionForm AddButton(string text, string? icon, Action<Player> onClick)
    {
        ArgumentNullException.ThrowIfNull(onClick);
        var state = new FormClickWork(onClick, _scope);
        using var label = InteropVariantText.CreateString(text);
        Interop.addButton(label, icon, FormClickWork.InvokePointer, state.UserData, FormClickWork.ReleasePointer);
        return this;
    }

    /// <summary>Sets the handler that runs when the player closes the form.</summary>
    public OwnedActionForm SetOnClose(Action<Player> onClose)
    {
        ArgumentNullException.ThrowIfNull(onClose);
        var state = new FormCloseWork(onClose, _scope);
        var form = Interop.AsForm_endstone__ActionForm_()
            ?? throw new InvalidOperationException("The carrier did not expose the form base.");
        form.setOnClose(FormCloseWork.InvokePointer, state.UserData, FormCloseWork.ReleasePointer);
        return this;
    }

    /// <summary>Sends the form to a player; the carrier keeps its own copy.</summary>
    public void Show(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        using var variant = InteropVariantForm.CreateActionForm(Interop);
        player.Interop.sendForm(variant);
    }

    /// <summary>Destroys the caller's copy. A shown form lives on in the carrier.</summary>
    public void Dispose()
    {
        var form = _form;
        if (form is null)
        {
            return;
        }

        _form = null;
        form.Dispose();
    }
}

internal sealed class FormClickWork
{
    private readonly GCHandle _self;
    private readonly BindingScope _scope;
    private Action<Player>? _handler;

    internal FormClickWork(Action<Player> handler, BindingScope scope)
    {
        _handler = handler;
        _scope = scope;
        _self = GCHandle.Alloc(this);
    }

    internal nint UserData => GCHandle.ToIntPtr(_self);

    internal static unsafe nint InvokePointer =>
        (nint)(delegate* unmanaged<nint, nint, void>)&FormTrampoline.Click;

    internal static unsafe nint ReleasePointer =>
        (nint)(delegate* unmanaged<nint, void>)&FormTrampoline.Release;

    internal void Run(nint player)
    {
        var handler = _handler;
        if (handler is null)
        {
            return;
        }

        try
        {
            handler(Player.From(InteropPlayer.Borrow(player), _scope));
        }
        catch (Exception exception)
        {
            _scope.Report(new BindingError(handler, exception));
        }
    }
}

internal sealed class FormCloseWork
{
    private readonly GCHandle _self;
    private readonly BindingScope _scope;
    private Action<Player>? _handler;

    internal FormCloseWork(Action<Player> handler, BindingScope scope)
    {
        _handler = handler;
        _scope = scope;
        _self = GCHandle.Alloc(this);
    }

    internal nint UserData => GCHandle.ToIntPtr(_self);

    internal static unsafe nint InvokePointer =>
        (nint)(delegate* unmanaged<nint, nint, void>)&FormTrampoline.Close;

    internal static unsafe nint ReleasePointer =>
        (nint)(delegate* unmanaged<nint, void>)&FormTrampoline.Release;

    internal void Run(nint player)
    {
        var handler = _handler;
        if (handler is null)
        {
            return;
        }

        try
        {
            handler(Player.From(InteropPlayer.Borrow(player), _scope));
        }
        catch (Exception exception)
        {
            _scope.Report(new BindingError(handler, exception));
        }
    }
}

internal static class FormTrampoline
{
    [UnmanagedCallersOnly]
    internal static void Click(nint userData, nint player)
    {
        if (GCHandle.FromIntPtr(userData).Target is FormClickWork work)
        {
            work.Run(player);
        }
    }

    [UnmanagedCallersOnly]
    internal static void Close(nint userData, nint player)
    {
        if (GCHandle.FromIntPtr(userData).Target is FormCloseWork work)
        {
            work.Run(player);
        }
    }

    [UnmanagedCallersOnly]
    internal static void Release(nint userData)
    {
        var handle = GCHandle.FromIntPtr(userData);
        if (handle.IsAllocated)
        {
            handle.Free();
        }
    }
}
