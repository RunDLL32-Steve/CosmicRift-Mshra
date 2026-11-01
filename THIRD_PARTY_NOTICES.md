# Third-party notices and provenance

The prototype's geometric ship, asteroid and HUD artwork is newly created for this project.

-   Godot Engine 4.5.1: MIT license, [official license](https://godotengine.org/license/) and [engine source](https://github.com/godotengine/godot/tree/4.5.1-stable). Distributed Windows builds include the engine license and its bundled third-party notices, exported by the build script.
-   .NET 8 runtime: Microsoft .NET, [license](https://github.com/dotnet/runtime/blob/v8.0.25/LICENSE.TXT) and [third-party notices](https://github.com/dotnet/runtime/blob/v8.0.25/THIRD-PARTY-NOTICES.TXT). Self-contained export includes runtime license/notices in its managed output where supplied by the SDK.
-   xUnit and Microsoft.NET.Test.Sdk: development/test dependencies only; not game assets.
-   [InfantryOnline / Infantry-Online-Server](https://github.com/InfantryOnline/Infantry-Online-Server) and [Zone-Assets](https://github.com/InfantryOnline/Zone-Assets): researched as original-file references. Code and asset reuse status must be established per repository and file; public availability alone does not establish redistribution rights. The historical initial prototype used newly created geometric assets. Local original-content exports now contain separately derived pixels from unchanged canonical source files; external native binaries remain ignored reference/working copies. Exact commit/path inventory is in `docs/original-research/INFANTRYONLINE.md`.
-   Cosmic Rift reference gameplay: [Infantry Online video](https://www.youtube.com/watch?v=A6u1ljFgQVU). Visual research reference; no video is included in the game distribution.
-   Watch skill from [bradautomates/claude-video](https://github.com/bradautomates/claude-video): installed separately in the user's Codex skills directory, MIT license; not a game dependency.

No rights to proprietary Cosmic Rift artwork, music, client code or trademarks are asserted. Asset import status remains separately tracked in the research and fidelity plan.

## Canonical local original-content package

The user-designated fowlmouth/cosmic-rift-assets source is pinned at eb1514eaae7f9b9425307d9cc043c74ecdd15e35. The local OriginalContent package includes derived sprite/map pixels and original-source/runtime JSON provenance; canonical source bytes/names/values are unchanged. This is a local development artifact, not a claim of external redistribution rights. No Infantry native client binary or upstream server implementation is included in this package. Independently written format decoders reference primary format facts; native package/tool provenance is in docs/architecture/NATIVE_CLIENT_PLAN.md.
