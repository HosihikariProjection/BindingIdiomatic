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

sealed class Disposable(int id, List<int> log) : IDisposable
{
    public int Id { get; } = id;

    public void Dispose() => log.Add(Id);
}
