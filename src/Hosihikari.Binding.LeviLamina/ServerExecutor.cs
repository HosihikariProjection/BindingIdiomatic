using System.Runtime.InteropServices;
using InteropServerThreadExecutor = Hosihikari.Bindings.LeviLamina.Interop.ll.thread.ServerThreadExecutor;

namespace Hosihikari.Binding.LeviLamina;

/// <summary>Runs server-thread work through the loader's executor.</summary>
/// <remarks>
/// The loader's default executor runs queued work on the server thread, which is the thread the
/// synchronization context treats as the server thread. It is a loader singleton and needs no
/// owner.
/// </remarks>
public sealed class ServerExecutor : IServerExecutor
{
    private readonly InteropServerThreadExecutor _executor;

    /// <summary>Creates an executor bound to the loader's default server-thread executor.</summary>
    public ServerExecutor()
    {
        _executor = InteropServerThreadExecutor.getDefault()
            ?? throw new InvalidOperationException("The loader's server-thread executor is not available.");
    }

    /// <inheritdoc/>
    public void Post(Action work)
    {
        ArgumentNullException.ThrowIfNull(work);
        var state = new ExecutorWork(work);
        _executor.execute(ExecutorWork.InvokePointer, state.UserData, ExecutorWork.ReleasePointer);
    }
}

internal sealed class ExecutorWork
{
    private readonly GCHandle _self;
    private Action? _work;

    internal ExecutorWork(Action work)
    {
        _work = work;
        _self = GCHandle.Alloc(this);
    }

    internal nint UserData => GCHandle.ToIntPtr(_self);

    internal static unsafe nint InvokePointer =>
        (nint)(delegate* unmanaged<nint, void>)&ExecutorTrampoline.Invoke;

    internal static unsafe nint ReleasePointer =>
        (nint)(delegate* unmanaged<nint, void>)&ExecutorTrampoline.Release;

    internal void Run()
    {
        var work = Interlocked.Exchange(ref _work, null);
        try
        {
            work?.Invoke();
        }
        catch
        {
            // Nothing can receive it across the native boundary. The synchronization context
            // reports its own callback failures; anything else posted here is the host's bug.
        }
    }
}

internal static class ExecutorTrampoline
{
    [UnmanagedCallersOnly]
    internal static void Invoke(nint userData)
    {
        if (GCHandle.FromIntPtr(userData).Target is ExecutorWork work)
        {
            work.Run();
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
