# Toolchain compatibility

Master reference: [§§13.1, 17, 18 Milestone 0, 25](../../ROGUE_TRADER_2_PLAN.md). Progress: [development checklist](../development-log/PROGRESS.md).

| Component | Frozen version | Rationale |
|---|---|---|
| Godot .NET editor + Windows templates | 4.5.1 stable, x64 | Version-matched engine, Godot.NET.Sdk and templates |
| .NET SDK | 8.0.419 | Compatible C# 12 SDK for Godot 4.5's .NET 8 support |
| Client, data, simulation, tests | net8.0 | Shared runtime/API baseline |
| xUnit | 2.9.3 | Critical simulation test runner |
| xUnit VS runner | 2.8.2 | `dotnet test` integration |
| Microsoft.NET.Test.Sdk | 17.12.0 | Test host |

Official sources: [Godot 4.5 C# prerequisites](https://docs.godotengine.org/en/4.5/tutorials/scripting/c_sharp/c_sharp_basics.html), [4.5.1 download archive](https://godotengine.org/download/archive/4.5.1-stable/), [Microsoft .NET 8 releases](https://dotnet.microsoft.com/en-us/download/dotnet/8.0), [release metadata with checksums](https://builds.dotnet.microsoft.com/dotnet/release-metadata/8.0/releases.json), [Godot binary release and digests](https://github.com/godotengine/godot-builds/releases/tag/4.5.1-stable).

The host initially had only SDK 7.0.400. Bootstrap installs an isolated SDK rather than targeting an unsupported old runtime. Downloads are SHA-512 (.NET) and SHA-256 (Godot) verified against official release metadata. Windows templates are extracted into the workspace and referenced by custom export paths, so no user-wide Godot installation is required.

This pinned baseline needs maintenance: .NET 8 LTS reaches end of support on November 10, 2026 according to [Microsoft support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core). Before introducing the persistent server or shipping a public release, choose a supported .NET LTS / Godot pair, update all pins and checksums together, and rerun tests plus exported-client validation. Do not silently update the SDK while leaving the engine/template versions mismatched.

Build reproducibility means fixed tools and gameplay inputs. Floating-point replay consistency is tested on this runtime; cross-architecture bit-identical lockstep is not claimed.
