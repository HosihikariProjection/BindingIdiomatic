namespace Hosihikari.Binding;

/// <summary>Schedules tasks onto the server thread.</summary>
/// <remarks>
/// <see cref="ServerSynchronizationContext"/> already carries <c>await</c> continuations back;
/// this scheduler is for explicit use — <c>Task.Factory.StartNew(..., scheduler)</c> or a
/// continuation that must run natively. There is no process-wide default scheduler to install:
/// .NET has none, so a task only lands here when the caller names it.
/// </remarks>
public sealed class ServerTaskScheduler : TaskScheduler
{
    private readonly ServerSynchronizationContext _context;

    /// <summary>Creates a scheduler that queues onto the given context.</summary>
    public ServerTaskScheduler(ServerSynchronizationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc/>
    protected override void QueueTask(Task task) => _context.Post(_ => TryExecuteTask(task), null);

    /// <inheritdoc/>
    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) =>
        _context.IsServerThread && TryExecuteTask(task);

    /// <inheritdoc/>
    protected override IEnumerable<Task> GetScheduledTasks() => [];
}
