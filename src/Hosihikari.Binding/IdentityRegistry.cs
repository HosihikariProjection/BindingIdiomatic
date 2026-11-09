using System.Runtime.CompilerServices;

namespace Hosihikari.Binding;

/// <summary>
/// Maps a native target pointer to one weakly held managed instance per generation.
/// Shared by every surface: the first caller wins, later callers receive the same instance.
/// </summary>
/// <remarks>
/// Entries carry the generation they were created under, because a freed native target
/// can be reused by an unrelated object. A query with a newer generation prunes the stale
/// entry instead of handing out an instance that already aliases a dead object.
/// The registry never keeps a managed instance alive on its own.
/// </remarks>
public sealed class IdentityRegistry<T> where T : class
{
    private readonly Dictionary<(nint Target, int Generation), WeakReference<T>> _entries = new();
    private readonly Lock _gate = new();

    /// <summary>Returns the registered instance for the target, or <see langword="null"/>.</summary>
    public T? TryGet(nint target, int generation)
    {
        if (target == 0)
        {
            return null;
        }

        lock (_gate)
        {
            if (!_entries.TryGetValue((target, generation), out var weak))
            {
                Prune(target, generation);
                return null;
            }

            if (weak.TryGetTarget(out var instance))
            {
                return instance;
            }

            _entries.Remove((target, generation));
            return null;
        }
    }

    /// <summary>
    /// Returns the registered instance, or registers and returns <paramref name="factory"/>'s result.
    /// </summary>
    public T GetOrAdd(nint target, int generation, Func<T> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        if (target == 0)
        {
            return factory();
        }

        lock (_gate)
        {
            if (_entries.TryGetValue((target, generation), out var weak) && weak.TryGetTarget(out var existing))
            {
                return existing;
            }

            Prune(target, generation);
            var created = factory();
            _entries[(target, generation)] = new WeakReference<T>(created);
            return created;
        }
    }

    /// <summary>Drops the entry for the target under any generation. Called on native destruction.</summary>
    public void Remove(nint target)
    {
        if (target == 0)
        {
            return;
        }

        lock (_gate)
        {
            List<(nint, int)>? stale = null;
            foreach (var key in _entries.Keys)
            {
                if (key.Target == target)
                {
                    (stale ??= []).Add(key);
                }
            }

            if (stale is null)
            {
                return;
            }

            foreach (var key in stale)
            {
                _entries.Remove(key);
            }
        }
    }

    /// <summary>Drops every entry. Called when a scope owning the registry is released.</summary>
    public void Clear()
    {
        lock (_gate)
        {
            _entries.Clear();
        }
    }

    private void Prune(nint target, int generation)
    {
        List<(nint, int)>? stale = null;
        foreach (var key in _entries.Keys)
        {
            if (key.Target == target && key.Generation != generation)
            {
                (stale ??= []).Add(key);
            }
        }

        if (stale is null)
        {
            return;
        }

        foreach (var key in stale)
        {
            _entries.Remove(key);
        }
    }
}
