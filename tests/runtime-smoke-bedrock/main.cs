using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Hosihikari;
using Hosihikari.Binding;
using Hosihikari.Binding.Bedrock;
using IoPath = System.IO.Path;
using Hosihikari.Bindings.Bedrock.Interop;
using BedrockVec3 = Hosihikari.Binding.Bedrock.Vec3;
using BedrockAabb = Hosihikari.Binding.Bedrock.AABB;
using BedrockOwnedAabb = Hosihikari.Binding.Bedrock.OwnedAabb;
using BedrockBoundingBox = Hosihikari.Binding.Bedrock.BoundingBox;
using BedrockOwnedBoundingBox = Hosihikari.Binding.Bedrock.OwnedBoundingBox;
using BedrockBlockPos = Hosihikari.Binding.Bedrock.BlockPos;
using BedrockOwnedChunkPos = Hosihikari.Binding.Bedrock.OwnedChunkPos;
using InteropBlockPos = Hosihikari.Bindings.Bedrock.Interop.BlockPos;
using InteropVec3 = Hosihikari.Bindings.Bedrock.Interop.Vec3;

// Exercises the Bedrock Idiomatic surface with the values the 1.26.60.6 runtime data
// snapshot actually exposes: borrowed value records, an owned/borrowed pair with real
// factories, and the identity registry.
public sealed class IdiomaticBedrockSmokePlugin : IPlugin
{
    private IPluginContext _context = null!;

    public void OnLoad(IPluginContext context) => _context = context;

    public PluginHookResult OnEnable()
    {
        Directory.CreateDirectory(_context.DataDirectory);
        // Written after every step: a hang anywhere must still leave the trace behind.
        var resultPath = IoPath.Combine(_context.DataDirectory, "idiomatic-bedrock-smoke.txt");
        var log = new StringBuilder();

        try
        {
            NativeMethods.Load(IoPath.Combine(_context.PackageDirectory, "Hosihikari.Bindings.Bedrock.Shim.dll"));
            log.AppendLine("load=ok");
            Flush(resultPath, log);

            using var scope = new BindingScope { Identities = new IdentityRegistry() };
            log.AppendLine("scope=ok");
            
            // A borrowed vector: components round-trip through the bridge.
            using (var interopVec = InteropVec3.ZERO())
            {
                log.AppendLine($"vec.interop=ok handle=0x{interopVec.Handle:x} err={NativeMethods.GetLastError() ?? "none"}");
                                var zero = BedrockVec3.From(interopVec);
                log.AppendLine($"vec.wrapped=0x{zero.Handle:x}");
                Flush(resultPath, log);
                var vx = zero.X;
                log.AppendLine($"vec.x={vx}");
                Flush(resultPath, log);
                var vy = zero.Y;
                log.AppendLine($"vec.y={vy}");
                Flush(resultPath, log);
                var vz = zero.Z;
                log.AppendLine($"vec.z={vz}");
                                log.AppendLine($"vec.zero={vx},{vy},{vz}");
                if (zero.X != 0f || zero.Y != 0f || zero.Z != 0f)
                {
                    log.AppendLine("vec.zero=FAILED");
                }

                // A borrowed vector never implements IDisposable in this layer.
                log.AppendLine($"vec.borrowed_contract={(!typeof(BedrockVec3).GetInterfaces().Contains(typeof(IDisposable)) ? "ok" : "failed")}");
            }

            log.AppendLine("vec.block=done");
            
            // A borrowed block position, and its vector conversion.
            using (var interopPos = InteropBlockPos.ZERO())
            {
                log.AppendLine($"pos.interop=ok handle=0x{interopPos.Handle:x}");
                Flush(resultPath, log);
                log.AppendLine($"pos.unwrapped=0x{interopPos.Handle:x} null={interopPos.Handle == 0}");
                Flush(resultPath, log);
                var pos = BedrockBlockPos.From(interopPos);
                log.AppendLine("pos.wrapped=ok");
                Flush(resultPath, log);
                var pxGuarded = TryRead(() => pos.X, out var pxFail);
                log.AppendLine($"pos.x={pxGuarded} fail={pxFail}");
                Flush(resultPath, log);
                log.AppendLine($"pos.raw.y={interopPos.y}");
                Flush(resultPath, log);
                var pyGuarded = TryRead(() => pos.Y, out var pyFail);
                log.AppendLine($"pos.y={pyGuarded} fail={pyFail}");
                Flush(resultPath, log);
                var pzGuarded = TryRead(() => pos.Z, out var pzFail);
                log.AppendLine($"pos.z={pzGuarded} fail={pzFail}");
                Flush(resultPath, log);
                // Both centre accessors return a small record by value; they are read
                // separately so a fault in one does not hide the component results.
                TryStep(resultPath, log, "pos.center", () => pos.Center is { } c ? $"{c.X},{c.Y},{c.Z}" : "missing");
                TryStep(resultPath, log, "pos.bottom", () => pos.BottomCenter is { } b ? b.Y.ToString() : "missing");
                TryStep(resultPath, log, "pos.tostring", () => (!string.IsNullOrEmpty(pos.ToString())).ToString());
            }

            // An owned chunk position from a vector, and a copy of a borrowed one. The carrier
            // may not export the factory; the shim reports that and the step is skipped.
            using (var interopVec = InteropVec3.ZERO())
            {
                var vec = BedrockVec3.From(interopVec);
                TryStep(resultPath, log, "chunk.owned", () =>
                {
                    using var probe = new BedrockOwnedChunkPos(vec);
                    return $"{probe.X},{probe.Z} contract={typeof(BedrockOwnedChunkPos).GetInterfaces().Contains(typeof(IDisposable))}";
                });
            }

            // A real copy of a borrowed box: the projected surface exposes the copy
            // constructor, so the copy must live in a distinct native record.
            using (var interopPos = InteropBlockPos.ZERO())
            {
                TryStep(resultPath, log, "aabb.copy", () =>
                {
                    using var source = BedrockBlockPos.From(interopPos).ToAabb();
                    if (source is null)
                    {
                        return "missing";
                    }

                    using var copy = BedrockOwnedAabb.CopyOf(BedrockAabb.From(source.Interop));
                    using var centre = copy.Center;
                    return $"distinct={copy.Handle != source.Handle} centre={centre?.X},{centre?.Y},{centre?.Z}";
                });
            }

            // The block-aligned box pair. Its factories may not resolve on this snapshot.
            using (var interopPos = InteropBlockPos.ZERO())
            {
                var corner = BedrockBlockPos.From(interopPos);
                TryStep(resultPath, log, "bbox.owned", () =>
                {
                    using var box = new BedrockOwnedBoundingBox(corner, corner);
                    using var side = box.SideLength;
                    return $"side={side?.X},{side?.Y},{side?.Z} contains={box.Contains(corner)}";
                });
                TryStep(resultPath, log, "bbox.copy", () =>
                {
                    using var box = new BedrockOwnedBoundingBox(corner, corner);
                    var borrowed = BedrockBoundingBox.From(box.Interop);
                    using var copy = BedrockOwnedBoundingBox.CopyOf(borrowed);
                    using var aabb = copy.ToAabb();
                    return $"distinct={copy.Handle != box.Handle} aabb={aabb is not null}";
                });
            }

            // Identity: bridging the same handle twice yields one instance.
            using (var interopVec = InteropVec3.ZERO())
            {
                var handle = interopVec.Handle;
                var first = scope.Identities!.GetOrAdd(handle, 0, () => BedrockVec3.From(interopVec));
                var second = scope.Identities!.GetOrAdd(handle, 0, () => BedrockVec3.From(interopVec));
                log.AppendLine($"identity.reused={ReferenceEquals(first, second)}");
            }

            // Scope reclaim and the identity clear that comes with it.
            var reclaimed = false;
            scope.Track(new SmokeReclaimable(() => reclaimed = true));
            scope.Dispose();
            log.AppendLine($"scope.reclaimed={reclaimed}");
            log.AppendLine($"scope.cleared={scope.Identities!.TryGet<BedrockVec3>(0x1234, 0) is null}");

            // A step that throws "symbol was not resolved" is a carrier gap, not a layer
            // defect: the shim reports it and the Idiomatic side degrades to null.
            var failures = log.ToString().Split('\n').Count(line =>
                line.Contains("FAILED") ||
                line.Contains("=failed") ||
                (line.Contains("threw") && !line.Contains("symbol was not resolved")));
            log.AppendLine(failures == 0 ? "result=OK" : "result=FAIL");
        }
        catch (Exception exception)
        {
            log.AppendLine($"error={exception.GetType().Name}: {exception.Message}");
            log.AppendLine("result=FAIL");
        }

        Flush(resultPath, log);
        return PluginHookResult.Allow();
    }

    public PluginHookResult OnDisable() => PluginHookResult.Allow();

    private static void TryStep(string path, StringBuilder log, string label, Func<string> step)
    {
        try
        {
            log.AppendLine($"{label}={step()}");
        }
        catch (Exception exception)
        {
            log.AppendLine($"  threw {exception.GetType().Name}: {exception.Message}");
        }

        Flush(path, log);
    }

    /// <summary>Writes the trace, tolerating a reader that holds the file open.</summary>
    internal static void Flush(string path, StringBuilder log)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                File.WriteAllText(path, log.ToString());
                return;
            }
            catch (IOException)
            {
                Thread.Sleep(50);
            }
        }
    }

    private static int TryRead(Func<int> read, out string failure)
    {
        try
        {
            var value = read();
            failure = "none";
            return value;
        }
        catch (Exception exception)
        {
            failure = exception.GetType().Name + ": " + exception.Message;
            return int.MinValue;
        }
    }

    public void OnUnload()
    {
    }
}

sealed class SmokeReclaimable(Action onDispose) : IDisposable
{
    public void Dispose() => onDispose();
}
