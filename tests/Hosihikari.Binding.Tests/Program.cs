using System.Collections.Concurrent;
using Hosihikari.Binding;

var failures = 0;

void Check(string name, bool condition)
{
    if (condition)
    {
        return;
    }

    failures++;
    Console.Error.WriteLine($"FAIL {name}");
}

// Identity: same target and generation returns the same instance.
var registry = new IdentityRegistry();
var first = registry.GetOrAdd(0x1000, 1, () => new Subject());
var second = registry.GetOrAdd(0x1000, 1, () => new Subject());
Check("identity is reused", ReferenceEquals(first, second));

// A newer generation prunes the old entry instead of returning the stale instance.
var nextGeneration = registry.GetOrAdd(0x1000, 2, () => new Subject());
Check("generation separates identity", !ReferenceEquals(first, nextGeneration));
Check("pruned generation is gone", registry.TryGet<Subject>(0x1000, 1) is null);

// Zero is never registered but still yields an instance.
Check("zero target yields an instance", registry.GetOrAdd(0, 1, () => new Subject()) is not null);

// Remove drops every generation for a target.
registry.Remove(0x1000);
Check("remove clears the target", registry.TryGet<Subject>(0x1000, 2) is null);

// Scope: reclaimables are disposed in reverse registration order.
var log = new List<int>();
var scope = new BindingScope();
scope.Track(new Disposable(1, log));
scope.Track(new Disposable(2, log));
scope.Dispose();
Check("dispose order is reversed", log is [2, 1]);
Check("scope reports disposed", scope.IsDisposed);

// Disposing twice is a no-op.
scope.Dispose();
Check("dispose is idempotent", log.Count == 2);

// Tracking after release disposes immediately rather than leaking.
var late = new Disposable(3, log);
scope.Track(late);
Check("late track disposes immediately", log is [2, 1, 3]);

// Untrack keeps the reclaimable out of the scope's set.
var kept = new Disposable(4, log);
var other = new BindingScope();
other.Track(kept);
other.Untrack(kept);
other.Dispose();
Check("untracked survives scope dispose", log is [2, 1, 3]);

// Require fails loudly without a live scope.
Check("require rejects a disposed scope", Throws<InvalidOperationException>(() => BindingScope.Require(scope)));

// A default scope is created per assembly and is stable.
Check("default scope is stable", ReferenceEquals(BindingScope.For(typeof(Program).Assembly), BindingScope.For(typeof(Program).Assembly)));

// Subscriptions run their unsubscribe exactly once.
var unsubscribes = 0;
var subscription = new Subscription(() => unsubscribes++);
subscription.Dispose();
subscription.Dispose();
Check("subscription unsubscribes once", unsubscribes == 1);
Check("subscription reports inactive", !subscription.IsActive);

// A throwing handler is isolated and reported instead of propagating.
var reportScope = new BindingScope();
BindingError? reported = null;
reportScope.Unhandled += error => reported = error;
var handlers = new List<Delegate>
{
    new Action<int>(_ => throw new InvalidOperationException("boom")),
    new Action<int>(value => Check("second handler still runs", value == 7)),
};
EventFiring.FireAll(reportScope, handlers, 7);
Check("failure was reported", reported?.Exception is InvalidOperationException);
Check("reported handler matches", reported?.Handler is Action<int>);

// Disposing the scope clears the identity registry it owns.
var identityScope = new BindingScope { Identities = new IdentityRegistry() };
identityScope.Identities.GetOrAdd(0x2000, 1, () => new Subject());
identityScope.Dispose();
Check("scope dispose clears identities", identityScope.Identities.TryGet<Subject>(0x2000, 1) is null);

// Server context: continuations posted from anywhere run on the installing thread.
using (var server = new FakeServer())
{
    var postedOn = 0;
    server.Context.Post(_ => postedOn = Environment.CurrentManagedThreadId, null);
    Check(
        "post runs on the server thread",
        SpinWait.SpinUntil(() => postedOn != 0, 2000) && postedOn == server.ThreadId);

    var sentOn = 0;
    server.Context.Send(_ => sentOn = Environment.CurrentManagedThreadId, null);
    Check("send blocks until the server runs it", sentOn == server.ThreadId);

    var scheduler = new ServerTaskScheduler(server.Context);
    var taskOn = 0;
    Task.Factory.StartNew(
        () => taskOn = Environment.CurrentManagedThreadId,
        CancellationToken.None,
        TaskCreationOptions.None,
        scheduler).Wait(2000);
    Check("task scheduler runs on the server thread", taskOn == server.ThreadId);
}

// A throwing continuation is reported instead of escaping onto the native caller.
var contextErrors = new List<Exception>();
using (var server = new FakeServer(contextErrors.Add))
{
    server.Context.Post(_ => throw new InvalidOperationException("boom"), null);
    Check(
        "continuation failure is reported",
        SpinWait.SpinUntil(() => contextErrors.Count == 1, 2000)
            && contextErrors[0] is InvalidOperationException);
}

Console.WriteLine(failures == 0 ? "OK" : $"{failures} failure(s)");
return failures == 0 ? 0 : 1;

static bool Throws<TException>(Action action) where TException : Exception
{
    try
    {
        action();
        return false;
    }
    catch (TException)
    {
        return true;
    }
}

sealed class Subject
{
}

sealed class FakeServer : IServerExecutor, IDisposable
{
    private readonly BlockingCollection<Action> _queue = new();
    private readonly Thread _thread;
    private readonly ManualResetEventSlim _ready = new();

    public FakeServer(Action<Exception>? onError = null)
    {
        _thread = new Thread(() =>
        {
            ThreadId = Environment.CurrentManagedThreadId;
            Context = ServerSynchronizationContext.Install(this, onError);
            _ready.Set();
            foreach (var work in _queue.GetConsumingEnumerable())
            {
                work();
            }
        })
        {
            IsBackground = true,
            Name = "fake-server",
        };
        _thread.Start();
        _ready.Wait();
    }

    public int ThreadId { get; private set; }

    public ServerSynchronizationContext Context { get; private set; } = null!;

    public void Post(Action work) => _queue.Add(work);

    public void Dispose()
    {
        _queue.CompleteAdding();
        _thread.Join();
    }
}

sealed class Disposable(int id, List<int> log) : IDisposable
{
    public int Id { get; } = id;

    public void Dispose() => log.Add(Id);
}
