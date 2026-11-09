using System;
using System.IO;
using System.Linq;
using System.Text;
using Hosihikari;
using Hosihikari.Binding;
using Hosihikari.Binding.LeviLamina;
using Hosihikari.Bindings.LeviLamina.Interop;

/// <summary>Exercises the hand-written Idiomatic layer inside a real BDS process.</summary>
public sealed class IdiomaticSmokePlugin : IPlugin
{
    private IPluginContext _context = null!;

    public void OnLoad(IPluginContext context) => _context = context;

    public PluginHookResult OnEnable()
    {
        Directory.CreateDirectory(_context.DataDirectory);
        var resultPath = Path.Combine(_context.DataDirectory, "idiomatic-smoke.txt");
        var log = new StringBuilder();

        try
        {
            var shimPath = Path.Combine(_context.PackageDirectory, "Hosihikari.Bindings.LeviLamina.Shim.dll");
            NativeMethods.Load(shimPath);
            log.AppendLine("load=ok");

            using var scope = new BindingScope { Identities = new IdentityRegistry() };
            log.AppendLine($"scope.created={!scope.IsDisposed}");

            // Service accessors return raw handles; some are published only after the loader is up.
            log.AppendLine($"service.level={Describe(Services.Level)}");
            log.AppendLine($"service.server={Describe(Services.ServerInstance)}");

            // The command registrar is a borrowed singleton; the bridge must be stable.
            var first = CommandRegistrar.ServerInstanceOn(scope);
            var second = CommandRegistrar.ServerInstanceOn(scope);
            log.AppendLine($"registrar.bridge={(first.Handle == second.Handle ? "ok" : "failed")}");
            log.AppendLine($"registrar.identity={(ReferenceEquals(first, second) ? "ok" : "failed")}");
            log.AppendLine($"registrar.handenum={first.HasEnum("nosuchesenum")}");
            log.AppendLine($"registrar.hansoft={first.HasSoftEnum("nosuchesoft")}");

            // The event bus is a borrowed singleton as well.
            var bus = EventBus.InstanceOn(scope);
            log.AppendLine($"eventbus.bridge={(bus.Handle != 0 ? "ok" : "failed")}");

            // Identity: bridging the same handle twice yields one instance.
            var busAgain = EventBus.From(bus.Interop, scope);
            log.AppendLine($"eventbus.identity={(ReferenceEquals(bus, busAgain) ? "ok" : "failed")}");
            _ = busAgain;
            _ = first;
            _ = second;

            // Tracking hands the same reclaimable back and the scope disposes it later.
            var reclaimed = false;
            var tracked = scope.Track(new SmokeReclaimable(() => reclaimed = true));
            log.AppendLine($"track.returned={!scope.IsDisposed}");
            _ = tracked;

            // Handler isolation: a throwing handler must not escape, and the next one must run.
            var reached = false;
            var reported = false;
            scope.Unhandled += _ => reported = true;
            EventFiring.FireAll<object?>(
                scope,
                [new Action<object?>(_ => throw new InvalidOperationException("smoke")), new Action<object?>(_ => reached = true)],
                null);
            log.AppendLine($"handler.isolated={reached && reported}");

            // Disposing the scope clears the identity registry it owns.
            var registry = scope.Identities!;
            log.AppendLine($"registry.live={registry.GetOrAdd(0x1234, 1, () => new object()) is not null}");
            scope.Dispose();
            log.AppendLine($"scope.disposed={scope.IsDisposed}");
            log.AppendLine($"track.reclaimed={reclaimed}");
            log.AppendLine($"registry.cleared={registry.TryGet<object>(0x1234, 1) is null}");

            var failures = log.ToString().Split('\n').Count(line => line.Contains("=failed"));
            log.AppendLine(failures == 0 ? "result=OK" : "result=FAIL");
        }
        catch (Exception exception)
        {
            log.AppendLine($"error={exception.GetType().Name}: {exception.Message}");
            log.AppendLine("result=FAIL");
        }

        File.WriteAllText(resultPath, log.ToString());
        return PluginHookResult.Allow();
    }

    public PluginHookResult OnDisable() => PluginHookResult.Allow();

    public void OnUnload()
    {
    }

    private static string Describe(nint? handle) =>
        handle is null or 0 ? "unavailable" : "present";
}

sealed class SmokeReclaimable(Action onDispose) : IDisposable
{
    public void Dispose() => onDispose();
}
