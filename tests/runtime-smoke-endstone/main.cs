using System;
using System.IO;
using System.Linq;
using System.Text;
using Hosihikari;
using Hosihikari.Binding;
using Hosihikari.Binding.Endstone;
using Hosihikari.Bindings.Endstone.Interop;
using InteropServer = Hosihikari.Bindings.Endstone.Interop.endstone.Server;
using InteropItemStack = Hosihikari.Bindings.Endstone.Interop.endstone.ItemStack;
using InteropItemType = Hosihikari.Bindings.Endstone.Interop.endstone.Identifier_endstone__ItemType_;

/// <summary>Exercises the Endstone Idiomatic surface inside a real Endstone process.</summary>
public sealed class IdiomaticEndstoneSmokePlugin : IPlugin
{
    private IPluginContext _context = null!;

    public void OnLoad(IPluginContext context) => _context = context;

    public PluginHookResult OnEnable()
    {
        Directory.CreateDirectory(_context.DataDirectory);
        var resultPath = Path.Combine(_context.DataDirectory, "idiomatic-endstone-smoke.txt");
        var log = new StringBuilder();

        try
        {
            var shimPath = Path.Combine(_context.PackageDirectory, "Hosihikari.Bindings.Endstone.Shim.dll");
            NativeMethods.Load(shimPath);
            log.AppendLine("load=ok");

            using var scope = new BindingScope { Identities = new IdentityRegistry() };

            var serverHandle = NativeMethods.GetServer();
            log.AppendLine($"server.handle={(serverHandle != 0 ? "present" : "missing")}");

            var server = Server.From(InteropServer.Borrow(serverHandle), scope);
            log.AppendLine($"server.name={server.Name}");
            log.AppendLine($"server.maxplayers={server.MaxPlayers}");
            log.AppendLine($"server.port={server.Port}");
            log.AppendLine($"server.version={server.Version}");
            log.AppendLine($"server.online={server.GetOnlinePlayers(scope).Count}");

            // Identity: bridging the same server twice yields one instance.
            var serverAgain = Server.From(InteropServer.Borrow(serverHandle), scope);
            log.AppendLine($"server.identity={(ReferenceEquals(server, serverAgain) ? "ok" : "failed")}");

            // Player lookups miss rather than throw for an absent name.
            log.AppendLine($"player.missing={(server.GetPlayer("nosuchplayer", scope) is null ? "ok" : "failed")}");

            // Owned item stack: creation, property folding, disposal.
            // Empty-handle regression: the shim returns a null handle when it cannot resolve a
            // symbol, and the surface maps that to null rather than to a dead wrapper.
            using var type = InteropItemType.Create("minecraft", "stone");
            log.AppendLine($"itemtype.created={type is not null}");
            if (type is null)
            {
                throw new InvalidOperationException($"the item type factory returned null: {NativeMethods.GetLastError() ?? "no reason recorded"}");
            }

            using (var stack = new OwnedItemStack(type, 3, 0))
            {
                log.AppendLine($"itemstack.amount={stack.Amount}");
                log.AppendLine($"itemstack.maxstack={stack.MaxStackSize}");
                log.AppendLine($"itemstack.key={stack.TranslationKey}");
                log.AppendLine($"itemstack.type={stack.Type.getTranslationKey()}");
            }

            log.AppendLine("itemstack.disposed=ok");

            // A borrowed stack never implements IDisposable in this layer.
            log.AppendLine($"itemstack.borrowed_contract={(!typeof(ItemStack).GetInterfaces().Contains(typeof(IDisposable)) ? "ok" : "failed")}");

            // Scope reclaim runs.
            var reclaimed = false;
            scope.Track(new SmokeReclaimable(() => reclaimed = true));

            // Handler isolation.
            var reached = false;
            var reported = false;
            scope.Unhandled += _ => reported = true;
            EventFiring.FireAll<object?>(
                scope,
                [new Action<object?>(_ => throw new InvalidOperationException("smoke")), new Action<object?>(_ => reached = true)],
                null);
            log.AppendLine($"handler.isolated={reached && reported}");

            scope.Dispose();
            log.AppendLine($"scope.reclaimed={reclaimed}");
            // Disposing the scope clears identities; a borrowed instance stays usable.
            log.AppendLine($"server.survives_scope={(server.Handle != 0 ? "ok" : "failed")}");

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
}

sealed class SmokeReclaimable(Action onDispose) : IDisposable
{
    public void Dispose() => onDispose();
}
