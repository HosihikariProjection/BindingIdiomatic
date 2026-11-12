# Hosihikari.Binding

[English](README.md) | [简体中文](README.zh_hans.md)

The hand-written .NET Idiomatic layer over the Hosihikari projection's Interop surfaces.

`Hosihikari.Binding` is the shared base; `Hosihikari.Binding.{Bedrock,Endstone,LeviLamina}` sit on top of the matching Interop assembly. Idiomatic types wrap Interop wrappers, expose properties, events, operators, indexers and constructors, and hand the wrapper back through `.Interop` when something was not carried over.

## Boundary

- **Plugin authors only.** The framework consumes the `.Interop` assemblies, never this layer, so a breaking change here does not touch the framework's compatibility axis.
- **Composition, not inheritance.** Nothing from the generated surface leaks into an Idiomatic signature, and the generator renaming its output does not change this layer's binary ABI.
- **No generation.** Everything here is handwritten. Coverage is an explicit per-surface manifest under `docs/`; anything not listed stays on Interop.
- **Lock-step versions with the Interop pair.** Both carry the same upstream version and binding revision, and the package depends on the exact Interop range.

## Owned and borrowed

Idiomatic mirrors ownership. A type reached only by borrowing does not implement `IDisposable`; one that can be owned does. A type with both paths always implements `IDisposable`, and disposing a borrowed instance only invalidates that instance — `ReleaseHandle` throws for it.

`From(interop)` is the bridge. It keeps one instance per native target per generation, and when the target is already bridged it absorbs the incoming wrapper: an owned handle transfers to the existing instance, a borrowed one is dropped.

## Scopes and threads

Every subscription is registered on a `BindingScope` and released with it, because a handler on a shared native object pins the load context. Without a scope, subscribing throws. The default is the calling assembly's scope; pass one explicitly to collect earlier.

Nothing switches threads for you. Game object affinity is documented on each member and asserted in Debug builds; cross-thread calls go through the server scheduler.

## Manifests

`docs/idiomatic-<surface>.tsv` has one row per type: the type's full name under the singular root, the forms it gets, and the members deliberately kept as methods with the reason. `scripts/verify_manifests.py` resolves every row against the checked-in Interop sources.

## Current state

The shared base carries the identity registry, scopes, event firing, the error channel and the server synchronization context with its task scheduler. The surfaces cover the Bedrock value types (vectors, block and chunk positions, the floating and block-aligned box pairs), the Endstone player/server/item/level/command/plugin types with managed event registration and action forms, and the LeviLamina service, command, event, hook and server-thread executor entry points.

Three runtime smokes load the layer inside a real server:

- `scripts/run-runtime-smoke.ps1 -ServerRoot <bds>` — LeviLamina carrier.
- `scripts/run-runtime-smoke-endstone.ps1 -ServerRoot <endstone bds>` — Endstone carrier. The root must have the framework's Endstone host deployed (`HostingShim/carriers/endstone/deploy.bat`) and the `endstone` CLI available through the venv named by `-Python`.
- `scripts/run-runtime-smoke-bedrock.ps1 -ServerRoot <bds> [-BedrockRoot <checkout>]` — Bedrock surface.
