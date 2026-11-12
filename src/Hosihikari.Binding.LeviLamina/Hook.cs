using System.Runtime.InteropServices;
using InteropFunctions = Hosihikari.Bindings.LeviLamina.Interop.ll.memory.Functions;

namespace Hosihikari.Binding.LeviLamina;

/// <summary>Priority of an installed hook; lower values run earlier.</summary>
public enum HookPriority
{
    /// <summary>Runs first.</summary>
    Highest = 0,

    /// <summary>Runs after <see cref="Highest"/>.</summary>
    High = 100,

    /// <summary>The default.</summary>
    Normal = 200,

    /// <summary>Runs after <see cref="Normal"/>.</summary>
    Low = 300,

    /// <summary>Runs last.</summary>
    Lowest = 400,
}

/// <summary>A function detour installed through the loader's hook library.</summary>
/// <remarks>
/// The native analogue of Paper's runtime instrumentation: the mechanism is portable, while the
/// target is a raw address resolved against a specific carrier build, so a hook is tied to that
/// carrier and version. Disposing removes the detour.
/// </remarks>
public sealed class Hook : IDisposable
{
    private readonly nint _target;
    private readonly nint _detour;
    private bool _installed;

    private Hook(nint target, nint detour, nint original)
    {
        _target = target;
        _detour = detour;
        Original = original;
        _installed = true;
    }

    /// <summary>Installs a detour, returning a token that calls the original when disposed.</summary>
    /// <param name="target">The address to hook.</param>
    /// <param name="detour">The function to run instead.</param>
    /// <param name="priority">Order among hooks on the same target.</param>
    /// <param name="suspendThreads">Whether the loader pauses threads while patching.</param>
    /// <exception cref="InvalidOperationException">The loader's hook library rejected the patch.</exception>
    public static Hook Attach(
        nint target,
        nint detour,
        HookPriority priority = HookPriority.Normal,
        bool suspendThreads = true)
    {
        if (target == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(target));
        }

        if (detour == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(detour));
        }

        var buffer = Marshal.AllocHGlobal(IntPtr.Size);
        try
        {
            Marshal.WriteIntPtr(buffer, 0);
            var result = InteropFunctions.hook(target, detour, buffer, (int)priority, suspendThreads);
            if (result != 0)
            {
                throw new InvalidOperationException(
                    $"Could not hook 0x{target:x}: the loader's hook library returned {result}.");
            }

            return new Hook(target, detour, Marshal.ReadIntPtr(buffer));
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    /// <summary>The address that was hooked.</summary>
    public nint Target => _target;

    /// <summary>The detour that was installed.</summary>
    public nint Detour => _detour;

    /// <summary>The trampoline that calls the original implementation.</summary>
    public nint Original { get; }

    /// <summary>Whether the detour is still installed.</summary>
    public bool IsInstalled => _installed;

    /// <summary>Removes the detour.</summary>
    public void Dispose()
    {
        if (!_installed)
        {
            return;
        }

        _installed = false;
        InteropFunctions.unhook(_target, _detour, suspendThreads: true);
    }
}
