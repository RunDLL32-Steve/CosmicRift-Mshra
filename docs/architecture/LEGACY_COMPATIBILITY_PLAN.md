# Legacy engine compatibility implementation plan

Master authority: [§1.2 original fidelity and §18 M0/M1](../../ROGUE_TRADER_2_PLAN.md). Reconcile completed evidence and discoveries with [PROGRESS.md](../development-log/PROGRESS.md); root owns the master checkboxes. The Godot prototype is a testbed while this gate evaluates original-engine compatibility. Canonical originals are never edited; no production multiplayer scope is introduced.

- [x] Clone InfantryOnline reference at `bb97579efeceff1c63d781d8c1bb8dd4a5b9b849` and create separate derived working source.
- [x] Pin official portable .NET 10.0.401 and verify Microsoft's SHA-512; isolate SDK selection from root .NET 8.
- [x] Copy seven canonical RT files into a separate pristine working zone and compare SHA-256; originals unchanged.
- [x] Recover five original help texts from corroborating pinned Zone-Assets archive, preserve separate reference copies, record URLs/SHA-256/length and verify working-copy equality. This completes dependency acquisition, not original help-menu behavior.
- [x] Acquire vendor-linked installer for static inspection: version 2.1.0.9, SHA-256 `e3770b201bda7df06c8e1f939cc57b66f248ef58a219468c00238e7c96e3577a`; not installed or executed.
- [x] Build actual pinned Assets library and compatibility loader; focused pristine parsing completed with controlled exit 0, six checks successful and source hashes unchanged.
- [x] Export actual parsed LVL tile physics/terrain metadata and LIO records for the root's fidelity renderer.
- [x] Compile current upstream ZoneServer: 1,320 upstream warnings, zero errors; separate derived probe compilation: 765 upstream warnings, zero errors.
- [ ] Resolve exact parser/version/command incompatibilities without changing original data; log every derived source patch and hashes.
- [x] Prepare separate database-delay-zero config, mandatory offline probe guard, disabled remote news/directory methods and fixed loopback game/ping bind sites in derived source. Source/config changes recorded with hashes; successful runtime socket binding remains unchecked below.
- [x] Validate successful standalone core-data startup and both actual loopback sockets after recovering original help dependencies: PID 36964 exited 0; game `127.0.0.1:21337`, ping `127.0.0.1:21338`, native ping player count/token verified; zero arenas/clients. Host exited and neither UDP port remained bound. This is a limited diagnostic-host feature, not native client/gameplay compatibility.
- [ ] Statically extract/identify an actual Infantry.exe engine from official distribution; installer version is not client version.
- [ ] Enter/unspectate original RT with native client, reproduce SilentClientCrash and compare original behavior; unresolved until real client evidence.
- [ ] Choose production engine after compatibility/parity evidence; master decision remains open.

Discoveries remain open tasks until verified:

- [ ] Current upstream server targets .NET 10; README legacy root solution is insufficient for its modern build.
- [ ] Active projectile parser has fixed columns despite item revision fields; all 200 original v57 item records parse, but original evaluator/client semantic parity still requires validation.
- [ ] Upstream LVL loader reads three tile layers but skips trailing graphical object data; decode that original layer before claiming complete map-object reconstruction.
- [ ] Original client player updates carry state/energy rather than only control inputs; reconcile master authority requirements before adopting emulator for production.
- [ ] Reconcile upstream wildcard game/ping binding and configuration semantics for any future production adoption. The bounded derived host has fixed and verified both loopback endpoints; upstream source remains unchanged.
- [ ] Original CFG help/menu dependencies `keys.txt`, `infcmdlst.txt`, `comms.txt`, `rules.txt`, `mods.txt` are absent from the canonical fowlmouth archive and server source. Original supplemental bytes were recovered from Zone-Assets; native help/menu behavior remains unverified. Do not invent replacement texts or edit original CFG.
- [ ] Resolve graphics/audio BLO dependency closure before native-client compatibility. The local core-data probe deliberately sets `loadBlobs=false`; it cannot establish native visual/audio compatibility.
- [ ] Validate generic CFG event dispatch including the `event` prefix and linked event names. The static switch-name audit alone reports prefixed event names as unmatched and must not be treated as unsupported-command proof.
- [ ] Upstream `Server.end()` cancels an outstanding socket receive, after which its callback rearms a disposed socket. Fix and verify this shutdown race before a durable production host; the bounded probe avoids that path and exits normally to release its process-owned sockets.

Evidence/working reports stay under ignored `.tools/compatibility/`, with provenance in `compatibility-manifest.json`. Reproducible owned code lives in `scripts/compatibility/`. Applicable upstream/client licenses remain separate provenance fields; local reference inspection does not imply redistribution.

## Loader validation incident — October 13, 2026

The first completed loader feature build succeeded with 168 upstream/runner nullable warnings and zero errors. Its focused validation run failed in the diagnostic exporter, before establishing a full load result: CFG public by-ref dictionary properties were unsupported by generic JSON serialization, and reusing a diagnostic record path collided with an upstream error path's retained stream. The latter escaped the runner and exited with `0xE0434352` (`-532462766`), the same code as the reported Windows application-error popup. That is a likely correlation; the screenshot's process was not independently identified. This is a runner defect, not proof that the canonical originals are damaged.

- [x] Runner fix: entry-point catch returning a nonzero exit code, unique diagnostic shard paths, and field-only export. Repaired build: zero errors; pristine run PID 22132 exited 0 with empty stderr. Wrong-argument PID 45816 returned 2. Two malformed recognized projectile records produced two distinct diagnostics; PID 28620 returned 1 with empty stderr. No unhandled managed termination in those checks.
- [x] Re-run focused completed loader check after repair: all original CFG/ITM/VEH/LVL/LIO/RPG inputs accepted, no source value/byte changes. This proves parser acceptance only, not original engine parity or a SilentClientCrash resolution.

No loader/server host remained running after the bounded checks. SDK10 build helpers are separately shut down during handoff. Canonical/source references were not edited. Build/run logs are retained at `.tools/compatibility/logs/pristine-loader-build.log` and `pristine-loader-run.log`.

## Verified compatibility evidence and remaining gate

The actual pinned parser accepted: CFG overhead mode; 200 ITM records at v57; 30 VEH records at v53; LVL 1999×1998 with 3,994,002 row-major tiles and 116,065 nonzero raw physics masks; 81 LIO records (5 parallax, 13 warp, 6 text, 15 sound, 42 hide); 12 RPG records at v6. The LVL export retains raw tile bytes and palette/material references; native coordinate stride 16 is sourced from upstream lookup code, with no conversion to prototype units. Upstream LVL trailing graphical objects remain undecoded.

Focused server initialization PID 46812 first returned controlled exit 2 with explicit missing-help-file diagnostics before socket startup. Research located the exact originals in [Zone-Assets Rogue archive](https://github.com/InfantryOnline/Zone-Assets/tree/401633b9b6829d9e5ae56b76a2563b1b95bbe4b9/Incompatible%20Zone%20Archives/SilentClientCrash/rogue). Five separate immutable reference copies and byte-identical working copies now close that dependency. Canonical fowlmouth files remain untouched.

With those originals present, PID 18400 initialized standalone, bound loopback and verified ping, but upstream `Server.end()` cancellation/rearm raised a managed worker exception during cleanup; the dedicated host's handler returned controlled exit 2. The completed host fix uses a normal bounded process exit for its background network workers, leaving upstream shutdown semantics unchanged. Its focused validation PID 36964 returned 0, with both real socket endpoints observed and the native eight-byte player-count/token response verified. Follow-up inventory confirms the host exited and ports 21337/21338 have zero remaining endpoints. No arena or player was created; no gameplay simulation occurred.

This is narrower than native engine compatibility. Game scripting/economics, all client graphics/audio, exact native client version, native entry/unspectating and SilentClientCrash remain unverified. The pristine upstream source still has wildcard bind sites; only the separate derived working source is patched.

Local artifacts (ignored, never redistributed):

- `reports/pristine-load-report.json`, `rogue-level-metadata.json`, `rogue-lio-records.json`, `pristine-items.json`, `pristine-vehicles.json`, `pristine-skills.json`.
- `reports/runner-error-regression.json`, `malformed-exception-regression.json`, malformed diagnostic shards and captured logs.
- `logs/zone-server-build.log`, `server-probe-build.log`, `server-probe.stdout.log`, `server-probe.stderr.log`.
- `reports/local-server-probe.json`, `local-server-probe-result.json`, `server-probe-cleanup.json`: bounded pass and release evidence; startup incident history is preserved separately.
- `compatibility-manifest.json`: pinned reference/canonical commits, SDK provenance, original/copied asset hashes and each derived source patch's original/working hashes.
- `native-client-package.json`: installer URL/version/SHA-256. Only installer bootstrap metadata is known; no actual native engine was extracted or executed.

Reproduce focused checks after the pinned reference, verified SDK and pristine working copies described above are prepared:

```powershell
./scripts/compatibility/run-data-loader.ps1 -Build
./scripts/compatibility/acquire-original-help.ps1
./scripts/compatibility/run-local-server-probe.ps1 -Build
```

The first command validates completed parsing. The dependency command downloads five original text files at the fixed archive commit; it executes no installer or native client. The final command prepares and validates the derived loopback/offline core-data host. Both executable checks use isolated SDK10 working directory/global.json, captured logs, hidden child windows, bounded 30-second waits and owned-process cleanup; node reuse is disabled. Only the specifically completed feature should trigger its focused check.

Next handoff: reconcile these narrow completions/discoveries with the master; verify BLO dependency closure and actual native engine package/version; trace RT CFG/item/script economy and versioned evaluators; then run a single completed native integration check against loopback and reproduce the archived SilentClientCrash. Engine choice stays open until that evidence exists. Do not execute the installer or begin further gameplay work as part of this handoff.
