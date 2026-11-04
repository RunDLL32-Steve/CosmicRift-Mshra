# Branch log: claude/project-thread-jzhlen (queue G6)

Scope: queue [G6](../../tasks/g6.md), the original LVL/LIO reader and collision grid, data only (RT-F021/F022 stay open). Result: `OriginalLevel` / `OriginalCollisionGrid` and `OriginalLio` in `shared/RogueTrader.OriginalData`, the [OriginalData entry](../../../shared/RogueTrader.OriginalData/plan.d/g6.md) and the oracle script `scripts/original-assets/audit_g6_level.py`.

Selection (November 3, 2026): Steve asked to start the next major item; the coordinator assigned G6. The only live claim was `claim/g6-level-reader`, two empty `CLAIM G6 ... by local` commits by Steve (RunDLL32) on October 15, 2026 with no work after them, so Steve's own request takes it over. That branch is left untouched. No overlap with C7 (Codex) or any open PR.

## Round 1 — November 3, 2026

-   Input boundary: LVL/Rogue.lvl (Git LFS in fowlmouth eb1514e, 1,290,994 bytes, SHA-256 `70344768…cf02f`) and LIO/Rogue.lio (already pinned). The LFS object was fetched read-only from the asset repository's LFS media URL; the same bytes are in InfantryOnline/Zone-Assets 401633b (see PR #109's ZONE_ASSETS_COMPARISON.md). No asset bytes are committed.
-   Layout used, from the InfantryOnline emulator reader (Infantry-Online-Server bb97579, `dotnetcore/Assets/Lvl/LvlInfo*.cs`; the header struct carries Rick/Gibbed's zlib licence; no code copied): 5,152-byte header, (objects + floors) × 64-byte blob references for version 6, three RLE planes (length byte, or 0 + big-endian 16-bit length, then value), then the entity table. The emulator stops before the entity table; this reader finds it is exactly EntityCount × 17 bytes (380,052 = 22,356 × 17) and keeps each record raw.
-   Only version 6 is accepted. Explicit failures: truncated header/blob table/plane, unsupported version, bad dimensions, zero-length run, run past the plane end, entity tail size mismatch. LIO: only `v23` and the ten emulator type numbers; the common ten header fields are typed, everything else stays raw by index.
-   Collision grid (emulator readings, RECONSTRUCTED): physics = plane 2 & 0x1F (non-zero blocks), vision = plane 2 >> 5, terrain = lookup[plane 0 & 0x7F] mod 16, 16-pixel tiles. Cross-check of two independent files: every refinery/trading-post hide in Rogue.lio lands on a terrain-15 tile and every repair dock on terrain 9, on clear tiles.
-   Tests: `tests/Unit/OriginalLevelTests.cs`, 23 cases (NEW synthetic fixtures for every failure mode; canonical header, blob references, plane and entity SHA-256s, histograms, LIO kinds). Canonical LVL tests skip with a reason when Rogue.lvl is still an LFS pointer.

Verified in the cloud container (SDK 8.0.133 substituted for the pinned 8.0.419; global.json change not committed): `dotnet test tests/Unit` with pinned assets and the LVL LFS content: 1045 passed (23 new), 0 failed, 5 existing skips. `audit_g6_level.py` reproduces every canonical oracle value.

Not verified / still open: native meaning of the entity records, plane 1 and plane-0 bit 7; OffsetX/OffsetY; PhysicsLow/High height semantics; LIO type-specific fields; native collision response. The version-5 orphan `roguetrader.lvl` (Zone-Assets) is not supported. No `verify.ps1` run (no client change).
