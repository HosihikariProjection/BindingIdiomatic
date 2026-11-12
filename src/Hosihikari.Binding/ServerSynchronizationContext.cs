using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;

namespace Hosihikari.Binding;

/// <summary>Routes asynchronous continuations back to the carrier's server thread.</summary>
/// <remarks>
/// <para>
/// Most of the native API may only be called on the server thread, so an <c>await</c> inside a
/// plugin must resume there. Installing this context on the server thread makes every
/// continuation posted by the compiler go through <see cref="IServerExecutor"/>.
/// </para>
/// <para>
/// Installation runs on the server thread and records it as the target; the host service owns
/// the lifetime and uninstalls before the carrier shuts down. <c>ConfigureAwait(false)</c>
/// bypasses the context, so any native call after it is no longer guaranteed to be on the
/// server thread.
/// </para>
/// </remarks>
public sealed class ServerSynchronizationContext : SynchronizationContext
{
    private readonly ConcurrentQueue<WorkItem> _queue = new();
    private readonly IServerExecutor _executor;
    private readonly Action<Exception>? _onError;
    private int _serverThreadId;
    private int _drainRequested;
    private SynchronizationContext? _previous;

    private ServerSynchronizationContext(IServerExecutor executor, Action<Exception>? onError)
    {
        _executor = executor;
        _onError = onError;
    }

    /// <summary>Installs the context on the calling thread, which becomes the server thread.</summary>
    /// <param name="executor">The carrier's way of running work on that thread.</param>
    /// <param name="onError">Receives a continuation that threw; when omitted the exception is swallowed.</param>
    /// <returns>The installed context.</returns>
    public static ServerSynchronizationContext Install(IServerExecutor executor, Action<Exception>? onError = null)
    {
        ArgumentNullException.ThrowIfNull(executor);
        var context = new ServerSynchronizationContext(executor, onError)
        {
            _serverThreadId = Environment.CurrentManagedThreadId,
            _previous = Current,
        };
        SetSynchronizationContext(context);
        return context;
    }

    /// <summary>Whether the calling thread is the one the context was installed on.</summary>
    public bool IsServerThread => Environment.CurrentManagedThreadId == _serverThreadId;

    /// <summary>Restores the context that was current before installation.</summary>
    /// <remarks>Call on the server thread; on any other thread it has no effect.</remarks>
    public void Uninstall()
    {
        if (Current == this)
        {
            SetSynchronizationContext(_previous);
        }
    }

    /// <inheritdoc/>
    public override void Post(SendOrPostCallback d, object? state)
    {
        ArgumentNullException.ThrowIfNull(d);
        _queue.Enqueue(new WorkItem(d, state));
        RequestDrain();
    }

    /// <inheritdoc/>
    public override void Send(SendOrPostCallback d, object? state)
    {
        ArgumentNullException.ThrowIfNull(d);
        if (IsServerThread)
        {
            d(state);
            return;
        }

        using var done = new ManualResetEventSlim();
        Exception? failure = null;
        Post(
            _ =>
            {
                try
                {
                    d(state);
                }
                catch (Exception exception)
                {
                    failure = exception;
                }
                finally
                {
                    done.Set();
                }
            },
            null);
        done.Wait();
        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    /// <inheritdoc/>
    public override SynchronizationContext CreateCopy() => this;

    private void RequestDrain()
    {
        // One executor task per batch: a burst of continuations must not schedule a task each.
        if (Interlocked.CompareExchange(ref _drainRequested, 1, 0) == 0)
        {
            _executor.Post(Drain);
        }
    }

    private void Drain()
    {
        while (true)
        {
            while (_queue.TryDequeue(out var item))
            {
                try
                {
                    item.Callback(item.State);
                }
                catch (Exception exception)
                {
                    _onError?.Invoke(exception);
                }
            }

            Volatile.Write(ref _drainRequested, 0);

            if (_queue.IsEmpty)
            {
                return;
            }

            // Work arrived while the flag was being cleared; another Post may already have
            // scheduled the next drain.
            if (Interlocked.CompareExchange(ref _drainRequested, 1, 0) != 0)
            {
                return;
            }
        }
    }

    private readonly record struct WorkItem(SendOrPostCallback Callback, object? State);
}
