using System.Reflection;
using System.Runtime.CompilerServices;

namespace Hosihikari.Binding.Endstone;

/// <summary>Registers managed handlers with the carrier's event manager.</summary>
/// <remarks>
/// The handler runs on the carrier's dispatching thread; this layer does not switch threads.
/// A throwing handler is reported to the registration scope and does not reach the native
/// caller.
/// </remarks>
public static class EventBus
{
    /// <summary>Registers a handler for an event name.</summary>
    /// <param name="owner">The plugin the registration belongs to; its lifetime bounds the native handler.</param>
    /// <param name="eventName">The carrier's event name.</param>
    /// <param name="handler">Called on the carrier's thread while the event is valid.</param>
    /// <param name="priority">Dispatch priority.</param>
    /// <param name="ignoreCancelled">Whether the handler still receives cancelled events.</param>
    /// <param name="scope">The scope that receives handler failures; defaults to the caller's.</param>
    public static EventSubscription Register(
        Plugin owner,
        string eventName,
        Action<Event> handler,
        EventPriority priority = EventPriority.Normal,
        bool ignoreCancelled = false,
        BindingScope? scope = null)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentException.ThrowIfNullOrEmpty(eventName);
        ArgumentNullException.ThrowIfNull(handler);

        var resolved = BindingScope.Require(scope ?? CallerScope());
        var manager = owner.Interop.getServer()?.getPluginManager()
            ?? throw new InvalidOperationException("The plugin has no server; the event cannot be registered.");

        var subscription = new EventSubscription(handler, resolved);
        manager.registerEvent(
            eventName,
            EventSubscription.InvokePointer,
            subscription.UserData,
            EventSubscription.ReleasePointer,
            (int)priority,
            owner.Interop,
            ignoreCancelled);
        return subscription;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static BindingScope CallerScope() => BindingScope.For(Assembly.GetCallingAssembly());
}
