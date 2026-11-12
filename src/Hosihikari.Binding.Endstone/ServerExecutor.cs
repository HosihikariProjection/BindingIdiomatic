using System.Runtime.InteropServices;
using InteropScheduler = Hosihikari.Bindings.Endstone.Interop.endstone.Scheduler;

namespace Hosihikari.Binding.Endstone;

/// <summary>Runs server-thread work through the carrier's scheduler.</summary>
/// <remarks>
/// The owner must be the host plugin: the carrier ties the queued task to that plugin and drops
/// it when the plugin is disabled. The task runs on the primary thread, which is the thread the
/// synchronization context treats as the server thread.
/// </remarks>
public sealed class ServerExecutor : IServerExecutor
{
    private readonly Plugin _owner;
    private readonly InteropScheduler _scheduler;

    /// <summary>Creates an executor bound to the host plugin.</summary>
    /// <param name="owner">The host plugin; its lifetime bounds the queued tasks.</param>
    public ServerExecutor(Plugin owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        _owner = owner;
        _scheduler = owner.Interop.getServer()?.getScheduler()
            ?? throw new InvalidOperationException("The plugin has no server; no scheduler is available.");
    }

    /// <inheritdoc/>
    public void Post(Action work)
    {
        ArgumentNullException.ThrowIfNull(work);
        var state = new SchedulerWork(work);
        _scheduler.runTask(_owner.Interop, SchedulerWork.InvokePointer, state.UserData, SchedulerWork.ReleasePointer);
    }
}

internal sealed class SchedulerWork
{
    private readonly GCHandle _self;
    private Action? _work;

    internal SchedulerWork(Action work)
    {
        _work = work;
        _self = GCHandle.Alloc(this);
    }

    internal nint UserData => GCHandle.ToIntPtr(_self);

    internal static unsafe nint InvokePointer =>
        (nint)(delegate* unmanaged<nint, void>)&SchedulerTrampoline.Invoke;

    internal static unsafe nint ReleasePointer =>
        (nint)(delegate* unmanaged<nint, void>)&SchedulerTrampoline.Release;

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

internal static class SchedulerTrampoline
{
    [UnmanagedCallersOnly]
    internal static void Invoke(nint userData)
    {
        if (GCHandle.FromIntPtr(userData).Target is SchedulerWork work)
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
