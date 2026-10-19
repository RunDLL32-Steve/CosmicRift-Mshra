# Persistence and transaction-integrity core (M2 foundation)

Status: **Implemented and verified by unit tests (see section 5, Implementation notes).** Authority: [master 9.3](../../ROGUE_TRADER_2_PLAN.md#93-transaction-safety), [14/14.1](../../ROGUE_TRADER_2_PLAN.md#14-persistent-data), [M2](../../ROGUE_TRADER_2_PLAN.md#milestone-2--rogue-trader-core-gameplay), [20.2](../../ROGUE_TRADER_2_PLAN.md#202-economy-tests); module plan [server/RogueTrader.Persistence](../../server/RogueTrader.Persistence/IMPLEMENTATION_PLAN.md). Completing this slice checks off **no** original-parity feature (RT-F068, RT-F035/F043/F049/F050/F054 stay open); it is the safe substrate the M2 "mine, sell, buy upgrade, exit and return" acceptance builds on.

The layer is **data-agnostic**: item/upgrade/hull/station IDs are opaque strings compared ordinally and stored verbatim (no trimming, case folding or normalization), currency is `long`, quantities are `int`, and every price, capacity and eligibility rule comes from caller-supplied interfaces. It contains no balance values. The only constants it defines are engineering retention limits (marked NEW below).

## 1. Project and types

`server/RogueTrader.Persistence/RogueTrader.Persistence.csproj`: `net8.0`, namespace `RogueTrader.Persistence`, **no** project or package references (uses BCL only: `System.Text.Json`, `System.Collections.Immutable`, `System.Security.Cryptography`, `TimeProvider`). Add it to `RogueTrader2.sln` under the `server` folder and add a `ProjectReference` from `tests/Unit/RogueTrader.UnitTests.csproj`. Nullable/implicit usings/LangVersion 12 come from `Directory.Build.props`. One public type per file; file names equal type names.

### 1.1 Profile model (immutable records)

```csharp
public sealed record PlayerProfile
{
    public const int CurrentSchemaVersion = 1;
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;   // must equal CurrentSchemaVersion in memory
    public required string ProfileId { get; init; }                   // ^[A-Za-z0-9_-]{1,64}$, not a Windows device name (used in file names)
    public long Revision { get; init; }                               // 0 at creation; +1 per committed save
    public long Wallet { get; init; }                                 // >= 0
    public ImmutableSortedDictionary<string, int> Inventory { get; init; } // Ordinal comparer; every value > 0; no zero entries
    public ImmutableSortedSet<string> Upgrades { get; init; }         // Ordinal comparer
    public string? ShipHullId { get; init; }                          // opaque; null until original hull data is integrated
    public ImmutableList<AppliedTransaction> AppliedTransactions { get; init; } // idempotency window, oldest first
    public ImmutableList<WalletLedgerEntry> Ledger { get; init; }     // oldest first, capped by LedgerRetention
    public long NextLedgerSequence { get; init; } = 1;
    public static PlayerProfile CreateNew(string profileId, long initialWallet, string? shipHullId); // empty collections, Ordinal comparers
}
public enum TransactionKind { Buy, Sell, BuyUpgrade, GrantReward }
public sealed record AppliedTransaction(string TransactionId, TransactionKind Kind, string RequestFingerprint,
    long CurrencyDelta, ImmutableSortedDictionary<string, int> ItemDeltas, string? UpgradeId,
    long WalletAfter, long Revision, DateTimeOffset AppliedAtUtc);
public sealed record WalletLedgerEntry(long Sequence, string TransactionId, TransactionKind Kind,
    long Delta, long BalanceAfter, DateTimeOffset AtUtc);
public sealed record PersistenceOptions
{
    public int IdempotencyWindow { get; init; } = 1024;  // NEW engineering limit, not game balance
    public int LedgerRetention { get; init; } = 1024;    // NEW engineering limit, not game balance
}
public static class ProfileInvariants
{   // Empty list == valid. Used on load (violation => Corrupt) and before every save (violation => Invalid).
    public static IReadOnlyList<string> Validate(PlayerProfile p, PersistenceOptions o);
}
```

Invariants: schema version current; ProfileId matches the regex; `Revision >= 0`; `Wallet >= 0`; inventory keys non-null/non-empty, values `> 0`; upgrade ids non-empty; transaction ids unique and non-empty (`<= 128` chars); window and ledger counts within limits; ledger sequences strictly increasing and `< NextLedgerSequence`. Record equality on collections is by reference, so tests compare through a `ProfileAssert.Equivalent` helper (tests project).

### 1.2 Repository

```csharp
public interface IProfileRepository
{
    Task<ProfileLoadResult> LoadAsync(string profileId, CancellationToken ct = default);
    Task<ProfileSaveResult> CreateAsync(PlayerProfile initial, CancellationToken ct = default);   // Revision must be 0; fails if any generation exists
    Task<ProfileSaveResult> SaveAsync(PlayerProfile next, long expectedRevision, CancellationToken ct = default);
}
public enum ProfileLoadStatus { Loaded, RecoveredFromBackup, NotFound, Corrupt, UnsupportedNewerVersion, MigrationMissing, IoError }
public sealed record ProfileLoadResult(ProfileLoadStatus Status, PlayerProfile? Profile, IReadOnlyList<PersistenceDiagnostic> Diagnostics)
{ public bool Succeeded => Status is ProfileLoadStatus.Loaded or ProfileLoadStatus.RecoveredFromBackup; }
public enum ProfileSaveStatus { Saved, RevisionConflict, AlreadyExists, Invalid, IoError }
public sealed record ProfileSaveResult(ProfileSaveStatus Status, long StoredRevision, IReadOnlyList<PersistenceDiagnostic> Diagnostics);
public enum DiagnosticCode { Truncated, MalformedJson, ChecksumMismatch, WrongFormat, InvariantViolation, NewerVersion,
    MigrationApplied, MigrationMissing, OrphanTempQuarantined, PrimaryQuarantined, BackupQuarantined, RecoveredFromBackup, IoFailure }
public sealed record PersistenceDiagnostic(DiagnosticCode Code, string Message, string? FilePath);
```

**Optimistic concurrency (CAS):** every repository tracks, per profile id, the last revision it loaded, created or saved (`ConcurrentDictionary<string,long>` guarded by a per-profile `SemaphoreSlim(1,1)`). `SaveAsync` returns `RevisionConflict` unless the tracked revision exists **and** equals `expectedRevision` **and** `next.Revision == expectedRevision + 1`. A save for a profile never loaded/created by this instance is a conflict. Exceptions are reserved for programmer errors (`ArgumentException`); all I/O and data failures come back as typed results.

`InMemoryProfileRepository : IProfileRepository` (same CAS rules, stores the immutable record) ships in the main project for tests and tooling.

### 1.3 FileProfileRepository (isolated local save)

`public sealed class FileProfileRepository : IProfileRepository, IDisposable` — `public static FileProfileRepository Open(string rootDirectory, PersistenceOptions options, IReadOnlyList<IProfileMigration> migrations, TimeProvider? time = null)`.

-   **Single owner:** `Open` creates the root and holds `<root>/.repository.lock` open with `FileShare.None` until `Dispose`; a second `Open` on the same root throws `ProfileRepositoryLockedException`. Cross-process sharing is out of scope.
-   **Files:** primary `<id>.profile.json`, backup `<id>.profile.bak.json`, temp `<id>.profile.json.tmp`, quarantine `<root>/quarantine/<id>.<yyyyMMddTHHmmssfffZ>.<DiagnosticCode>.json`. Files are **moved** into quarantine, never deleted.
-   **Format (envelope, UTF-8, no BOM):** `{"format":"rogue-trader.player-profile","schemaVersion":1,"sha256":"<lowercase hex>","payload":"<payload JSON as a string>"}`. `sha256` is over the UTF-8 bytes of the `payload` string, so the checksum never depends on re-serialization. The payload is `ProfileDocumentV1` (internal DTO with plain `Dictionary<string,int>`/`List<>` members, camelCase, `UnmappedMemberHandling.Disallow` like `GameCatalog`) mapped explicitly to/from `PlayerProfile`. `ProfileSerializer` (public static) exposes `byte[] Serialize(PlayerProfile)` and `ProfileDecodeResult Decode(ReadOnlySpan<byte>, IReadOnlyList<IProfileMigration>)`.
-   **Decode order:** parse envelope (failure: `Truncated` if the bytes end before the root object closes, else `MalformedJson`) -> `format` check (`WrongFormat`) -> `schemaVersion > Current` => `NewerVersion` (stop; nothing else inspected) -> checksum (`ChecksumMismatch`) -> payload parse -> if `schemaVersion < Current` run migrations -> map -> `ProfileInvariants` (`InvariantViolation`).
-   **Migration hook:** `public interface IProfileMigration { int FromVersion { get; } JsonObject Migrate(JsonObject payload); }` (produces `FromVersion + 1`). `public static class ProfileMigrator { MigrationOutcome Upgrade(JsonObject payload, int fromVersion, int targetVersion, IReadOnlyList<IProfileMigration> steps); }` applies steps in order; a gap yields `MigrationMissing`. Version 1 ships with an empty list. A migrated profile is written at the current version on its next save (the old file becomes the backup naturally).
-   **Atomic save:** under the per-profile lock: CAS check -> invariants -> serialize -> write temp with `new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough)`, `Write`, `Flush(flushToDisk: true)`, dispose -> if primary exists `File.Replace(tmp, primary, backup, ignoreMetadataErrors: true)` else `File.Move(tmp, primary)` -> update tracked revision -> `Saved`. Any exception => `IoError` with the tracked revision unchanged. `CreateAsync` uses the same path and returns `AlreadyExists` if primary, backup or temp exists.
-   **Load algorithm:** (1) if a temp file exists, quarantine it (`OrphanTempQuarantined`) — it was never acknowledged. (2) primary present and decodes => `Loaded`. (3) primary `NewerVersion` or `MigrationMissing` => return that status, touch **nothing**, no backup fallback (falling back would silently regress progress). (4) primary corrupt (any other code) => quarantine it (`PrimaryQuarantined`), go to (5). (5) backup present and decodes => `RecoveredFromBackup` (backup copied to primary via the atomic save path at its own revision; diagnostics list everything). Backup corrupt => quarantine it, return `Corrupt`. (6) neither file present and nothing quarantined in this call => `NotFound`; if anything was quarantined => `Corrupt`. **No path returns an empty/new profile**; only the caller creates one, and only after `NotFound`.

### 1.4 Transaction layer

```csharp
public sealed record TransactionContext(string AuthenticatedProfileId, bool StationAccessVerified); // caller checks docking/proximity
public sealed record BuyRequest(string TransactionId, string ProfileId, string StationId, string ItemId, int Quantity);
public sealed record SellRequest(string TransactionId, string ProfileId, string StationId, string ItemId, int Quantity);
public sealed record BuyUpgradeRequest(string TransactionId, string ProfileId, string StationId, string UpgradeId);
public sealed record GrantRewardRequest(string TransactionId, string ProfileId, string SourceId, long Currency,
    IReadOnlyDictionary<string, int> Items);   // mined cargo collection and NPC rewards both use this
public interface IPriceSource   // authoritative; false = not traded here; called once per transaction inside the lock
{
    bool TryGetBuyUnitPrice(string stationId, string itemId, out long unitPrice);
    bool TryGetSellUnitPrice(string stationId, string itemId, out long unitPrice);
    bool TryGetUpgradePrice(string stationId, string upgradeId, out long price);
}
public interface ICargoPolicy   // capacity/weight/stacking live with original data; called only when any quantity increases
{
    bool Fits(PlayerProfile current, IReadOnlyDictionary<string, int> proposedInventory);
}
public enum TransactionStatus { Applied, Rejected }
public enum RejectionReason { None, InvalidRequest, IdentityMismatch, ProfileNotOpen, TransactionIdReused, StationAccessDenied,
    NotForSale, InvalidPrice, InsufficientInventory, InsufficientFunds, UpgradeAlreadyOwned, CargoCapacityExceeded,
    ArithmeticOverflow, PersistenceConflict, PersistenceFailed }
public sealed record TransactionResult(string TransactionId, TransactionStatus Status, RejectionReason Reason, bool IsReplay,
    long CurrencyDelta, IReadOnlyDictionary<string, int> ItemDeltas, string? UpgradeId, long WalletAfter, long Revision);
public interface ITransactionProcessor
{
    Task<ProfileLoadResult> OpenProfileAsync(string profileId, CancellationToken ct = default); // (re)loads from repository
    PlayerProfile? GetSnapshot(string profileId);                                                 // last committed state
    Task<TransactionResult> BuyAsync(TransactionContext ctx, BuyRequest r, CancellationToken ct = default);
    Task<TransactionResult> SellAsync(TransactionContext ctx, SellRequest r, CancellationToken ct = default);
    Task<TransactionResult> BuyUpgradeAsync(TransactionContext ctx, BuyUpgradeRequest r, CancellationToken ct = default);
    Task<TransactionResult> GrantRewardAsync(TransactionContext ctx, GrantRewardRequest r, CancellationToken ct = default);
}
public sealed class TransactionProcessor(IProfileRepository repository, IPriceSource prices, ICargoPolicy cargo,
    PersistenceOptions options, TimeProvider? time = null) : ITransactionProcessor;
```

**Per-profile serialization:** the processor holds `ConcurrentDictionary<string, ProfileSlot>`; `ProfileSlot` = `SemaphoreSlim(1,1)`, `PlayerProfile Committed`, `bool NeedsReload`. Every transaction and `OpenProfileAsync` runs entirely inside the slot lock (the token is honored only while waiting for it; commit uses `CancellationToken.None`). Within one processor that alone prevents double-spend; the repository CAS additionally rejects a second processor or stale writer (`PersistenceConflict`), so two writers can never both commit from the same revision.

**Validation order (fixed; first failure wins; no state touched before step 8):** 0. Shape: ids non-null/non-empty (TransactionId <= 128 chars), `Quantity > 0`, reward `Currency >= 0`, reward item quantities `> 0`, reward not entirely empty => `InvalidRequest`.

1. Identity (9.3-1): `r.ProfileId == ctx.AuthenticatedProfileId` (Ordinal) else `IdentityMismatch`; slot exists and `!NeedsReload` else `ProfileNotOpen`. Acquire lock.
2. Duplicate lookup (9.3-6, read side): if `TransactionId` is in the window: same `RequestFingerprint` => return the stored result with `IsReplay = true`, no save; different fingerprint => `TransactionIdReused`. This precedes state checks because a replay must return the original outcome even if funds/cargo have since changed. Fingerprint = lowercase hex SHA-256 of `kind|profileId|stationOrSource|itemOrUpgrade|quantity|currency|items` with reward items sorted ordinally as `id=qty` joined by `;`.
3. Proximity (9.3-2): Buy/Sell/BuyUpgrade require `ctx.StationAccessVerified` else `StationAccessDenied`; rewards skip it.
4. Inventory (9.3-3): Sell needs `held >= Quantity` else `InsufficientInventory`; BuyUpgrade on an owned id => `UpgradeAlreadyOwned`.
5. Price (9.3-4): `Try...` false => `NotForSale`; negative price => `InvalidPrice`; `total = checked(unit * (long)Quantity)`.
6. Currency (9.3-3): Buy/BuyUpgrade need `Wallet >= total` else `InsufficientFunds`; Sell/Reward `checked(Wallet + credit)`. Inventory adds `checked(held + qty)` as `int`. Any `OverflowException` => `ArithmeticOverflow`.
7. Cargo: if any quantity increases, `cargo.Fits(committed, proposedInventory)` false => `CargoCapacityExceeded` (rewards are all-or-nothing; no partial grant).
8. Build `next` (9.3-5, atomic in memory): new inventory (zero entries removed), wallet, upgrades, `Revision + 1`, append `AppliedTransaction` (evict oldest beyond `IdempotencyWindow`) and one `WalletLedgerEntry` (Delta may be 0; evict beyond `LedgerRetention`).
9. Persist (9.3-7): `SaveAsync(next, committed.Revision)`. `Saved` => `Committed = next`, return `Applied`. **Acknowledge only after the save returns.** `RevisionConflict` => `PersistenceConflict`; `IoError`/`Invalid`/exception => `PersistenceFailed`. On both, `Committed` is untouched (rollback is free: the record is immutable) and `NeedsReload = true`, so later calls get `ProfileNotOpen` until `OpenProfileAsync` re-reads disk truth. If the failed write actually landed, the retry with the same id then returns the original result as a replay.

Rejections are not recorded; a rejected id may be retried. Callers must generate unique ids (e.g. `Guid.NewGuid().ToString("N")`); a replay older than the window is indistinguishable from a new request (documented limit).

## 2. Failure and recovery matrix

| Situation                                                 | Disk state                        | Load outcome                                            | Player-visible effect                                           |
| --------------------------------------------------------- | --------------------------------- | ------------------------------------------------------- | --------------------------------------------------------------- |
| Crash while writing temp / before `Replace`               | old primary, partial or full temp | `Loaded` (old), temp quarantined                        | Unacknowledged transaction absent; client retry applies it once |
| Crash inside `Replace` leaving no primary                 | backup (old), maybe temp          | temp quarantined, `RecoveredFromBackup`                 | Same as above                                                   |
| Crash after `Replace`, before ack                         | new primary, backup               | `Loaded` (new)                                          | Retry with same id returns stored result (`IsReplay`)           |
| Truncated primary                                         | primary short                     | primary quarantined, `RecoveredFromBackup` or `Corrupt` | At most the last generation lost, always reported               |
| Bad checksum / invariant violation (e.g. negative wallet) | primary edited/damaged            | as truncated                                            | as truncated                                                    |
| Primary and backup both corrupt                           | both bad                          | both quarantined, `Corrupt`                             | Game must not start a fresh profile; surfaces error             |
| Newer schema version                                      | primary from newer build          | `UnsupportedNewerVersion`, nothing moved                | Error; saving impossible (not loaded)                           |
| Older version, no migration                               | primary v<current                 | `MigrationMissing`, nothing moved                       | Error                                                           |
| Duplicate transaction after restart                       | window persisted in profile       | —                                                       | Replay returns original result, no second credit                |
| Save I/O error (disk full, locked)                        | primary unchanged                 | —                                                       | `PersistenceFailed`, in-memory rolled back, reopen required     |

## 3. Test plan (xunit, `tests/Unit/Persistence/`, namespace `RogueTrader.UnitTests.Persistence`)

Fixtures: `FixturePriceSource` (dictionary-backed; every price labelled `// NEW provisional test fixture, not game balance`), `FixtureCargoPolicy` (sum of units <= a NEW fixture limit), `FaultInjectingProfileRepository` (wraps `InMemoryProfileRepository`; modes `FailBeforeWrite`, `ThrowAfterWrite`), `TempDirectory : IDisposable`, `ProfileAssert.Equivalent`. Item ids are obviously synthetic (`fixture-ore-a`, `fixture item/Ω #2`). Test classes: `EconomyTransactionTests`, `TransactionConcurrencyTests`, `FileProfileRepositoryTests`, `ProfileSerializerTests`.

20.2 bullet -> tests:

1. _Cannot spend money they do not have:_ `Buy_WithInsufficientFunds_IsRejectedAndProfileUnchanged`, `BuyUpgrade_WithInsufficientFunds_IsRejected`.
2. _Cargo cannot become negative:_ `Sell_MoreThanHeld_IsRejectedAndInventoryUnchanged`, `Sell_ItemNotHeld_IsRejected`, `Sell_AllUnits_RemovesEntry`, `NonPositiveQuantity_IsRejected` (Theory: 0, -1, int.MinValue).
3. _Duplicate requests create no duplicate rewards:_ `GrantReward_DuplicateId_IsNoOpReturningOriginalResult`, `GrantReward_DuplicateAfterRestart_IsNoOp`, `Buy_DuplicateId_DoesNotChargeTwice`, `TransactionId_ReusedWithDifferentPayload_IsRejected`.
4. _Prices calculated correctly:_ `Buy_ChargesAuthoritativeUnitPriceTimesQuantity`, `Sell_CreditsAuthoritativeUnitPriceTimesQuantity`, `Buy_UnpricedItem_IsRejectedAsNotForSale`, `NegativePrice_IsRejectedAsInvalidPrice`, `Buy_TotalPriceOverflow_IsRejected`, `Sell_WalletOverflow_IsRejected`, `Applied_AppendsLedgerEntryWithBalanceAfter`.
5. _Save/load preserves currency and inventory:_ `SaveLoad_RoundTripPreservesWalletInventoryUpgradesHullAndWindow`, `SaveLoad_PreservesOpaqueIdsVerbatim`, `ExitAndReturn_RewardSellUpgrade_ProgressionIntact` (reward cargo, sell, buy upgrade, dispose processor and repository, reopen, verify).
6. _Simultaneous transactions cannot double-spend:_ `ConcurrentBuys_OnlyAffordableOnesApply` (64 parallel buys, funds for N; exactly N applied, wallet never negative), `ConcurrentDuplicateRewards_ApplyExactlyOnce`, `Repository_ConcurrentSavesSameExpectedRevision_OnlyOneSucceeds`, `TwoProcessorsSharingRepository_CannotDoubleSpend`.

Validation/integrity: `Identity_Mismatch_IsRejected`, `StationAccess_NotVerified_IsRejected`, `GrantReward_DoesNotRequireStationAccess`, `Buy_ExceedingCargoPolicy_IsRejected`, `GrantReward_ExceedingCargoPolicy_IsRejectedWhole`, `BuyUpgrade_AlreadyOwned_IsRejected`, `IdempotencyWindow_EvictsOldestBeyondCapacity`, `SaveFailure_RollsBackAndRequiresReopen`, `SaveThrowsAfterWrite_RetryAfterReopenReturnsReplay`.

Persistence: `Load_Missing_ReturnsNotFound`, `Create_WhenAnyGenerationExists_ReturnsAlreadyExists`, `Save_StaleRevision_ReturnsConflict`, `Save_WithoutPriorLoad_ReturnsConflict`, `Save_KeepsPreviousGenerationAsBackup`, `Load_OrphanTemp_UsesPrimaryAndQuarantinesTemp`, `Load_PrimaryMissingWithBackup_RecoversFromBackup`, `Load_TruncatedPrimary_QuarantinesAndRecoversFromBackup`, `Load_BadChecksum_QuarantinesAndRecoversFromBackup`, `Load_InvariantViolation_TreatedAsCorrupt`, `Load_PrimaryAndBackupCorrupt_ReturnsCorruptNeverEmptyProfile`, `Load_NewerSchemaVersion_ReturnsUnsupportedAndLeavesFilesUntouched`, `Migrator_AppliesStepsInOrder`, `Migrator_MissingStep_ReportsMigrationMissing` (synthetic versions passed to `ProfileMigrator.Upgrade`), `Payload_UnknownField_IsRejected`, `ProfileId_WithPathCharacters_IsRejected`, `Repository_SecondOpenOnSameRoot_IsRefused`.

For persistence-only changes, run `dotnet test tests/Unit` using the pinned environment. `scripts/verify.ps1` additionally builds/imports/smokes the Godot client and is required when client/shared-physics changes or release scope calls for it. Each persistence test uses its own temp directory.

## 4. Non-goals and open questions

Non-goals: PostgreSQL, schema migrations for a database, accounts/authentication, multiplayer or cross-process save sharing, original economy parity (prices, capacities, item lists, starting wallet, sell/buy spreads, the 9.4 currency-creation check — the latter belongs to price data), repairs/resupply, hull exchange, death/drop retention, sector transfer, UI, Godot wiring, cloud backup/rotation beyond one previous generation.

Open questions (recorded, not blocking this slice):

-   **Original item data:** the later integration supplies `IPriceSource`, `ICargoPolicy` (capacity, unit weight, stacking, which items count as cargo) and the hull id from data/original; IDs pass through verbatim. Whether the original kept currency as signed 32-bit and how to clamp/import is RT-F035/F049 evidence work.
-   **Proximity/docking:** the simulation/server computes `StationAccessVerified`; the M2 integration decides and wires its station-interaction rule. The compatibility packet-shop path now has the separate RT-F389 StoreEnabled/spectator guard; that does not wire this persistence core into the native server.
-   **Upgrade installation restrictions** (hull/slot eligibility) are not modelled; until then `TryGetUpgradePrice` returning false is the only gate. A later `IUpgradeEligibility` may be inserted between steps 4 and 5.
-   **Retained state on death/logout/transfer** (RT-F043/F050/F054) decides which profile fields survive; this layer persists whatever the caller commits.
-   **PostgreSQL (M3):** implement `IProfileRepository` with row-version CAS and a unique `(profile_id, transaction_id)` constraint; the processor contract is unchanged.
-   **Window size:** 1024 is a NEW engineering default; revisit once real transaction rates are known.

## 5. Implementation notes

Implemented in `server/RogueTrader.Persistence` with tests in `tests/Unit/Persistence/` (every test named in section 3 exists; additional cases cover overflow, shape, cargo-on-sell, quarantine naming and stale processors). Smallest-safe choices where the design was ambiguous or had a gap:

1. **Orphan temp does not make a profile Corrupt.** Load step 6 says "anything quarantined => Corrupt". A temp file left by a crash during the very first `Create` was never acknowledged, so quarantining it alone yields `NotFound` (diagnostics still report `OrphanTempQuarantined`). Only a quarantined primary or backup yields `Corrupt`.
2. **Corrupt must stay Corrupt.** After a first load quarantined both generations and returned `Corrupt`, a second load would otherwise see no files and return `NotFound`, inviting a fresh profile over lost progress. `FileProfileRepository` now also returns `Corrupt` (diagnostic `PrimaryQuarantined`, path = quarantine folder) when no live file exists but the quarantine folder holds a primary/backup generation of that id (`<id>.<stamp>.<Code>.json`, excluding `OrphanTempQuarantined`), and `CreateAsync` returns `AlreadyExists` in that state. An operator resolves it by moving the quarantined files out.
3. **Quarantine name collisions.** Two files quarantined in the same millisecond with the same code get a numeric `.2`, `.3` ... suffix before `.json`; files are still never deleted.
4. **Fingerprint is length-prefixed.** Variable-length fields are encoded as `<length>:<value>` (still `|`-joined, items `;`-joined and sorted ordinally) so ids containing separators cannot collide. The hash remains lowercase hex SHA-256.
5. **Decode checks invariants with unbounded window/retention** (`Decode` has no options parameter). A narrower configured `IdempotencyWindow`/`LedgerRetention` therefore never quarantines good data; the next transaction trims the lists (eviction loops until within limits). `Save` still enforces the configured limits.
6. **Invalid profile ids**: `LoadAsync` (and `OpenProfileAsync`) throw `ArgumentException`; `CreateAsync`/`SaveAsync` return `Invalid` because the id comes from the profile being validated. The id check is manual (a regex `$` would accept a trailing newline).
7. **`ProfileSaveResult.StoredRevision`** is the saved revision on success, otherwise the repository's tracked revision or -1 when none is tracked. `TransactionResult.Revision` is -1 (and `WalletAfter` 0) for rejections issued before the profile state is known (shape, identity, not open).
8. **Transaction pipeline detail:** `ProfileNotOpen` is also re-checked under the lock (another caller may have set `NeedsReload` while waiting). A failed `OpenProfileAsync` (anything but `Loaded`/`RecoveredFromBackup`) leaves the slot `NeedsReload` so no transaction runs on unknown disk state. A migration step that throws propagates to the caller instead of quarantining the file.
9. **Version 0 files** (schemaVersion below 1) report `MigrationMissing` because version 1 ships with no migration steps; the migration hook itself is covered with synthetic versions and a synthetic version-0 step in the tests.

Review fixes (independent review, October 15, 2026; regression tests in `ProfileIdentityAndEncodingTests`):

10. **Profile identity on disk.** A readable primary/backup whose payload `profileId` differs (ordinal) from the requested id returns `Corrupt` (diagnostic `InvariantViolation`) and is left in place, never quarantined, because it may be another profile's live save. Before this, on a case-insensitive file system (Windows) `Load("alice")` returned `Alice`'s profile and a processor spent `Alice`'s wallet. The file repository's per-profile locks use `OrdinalIgnoreCase` so case variants cannot touch the same files concurrently; tracked revisions stay ordinal. `OpenProfileAsync` also refuses (as `Corrupt`) any repository result whose id differs.
11. **Reserved device names.** `CON`, `PRN`, `AUX`, `NUL`, `COM0`-`COM9`, `LPT0`-`LPT9` (any case) are invalid profile ids on every platform: on Windows `<name>.profile.json` addresses the device, not a file.
12. **Well-formed UTF-16 ids.** Request ids (transaction, station/source, item, upgrade, reward items) with an unpaired surrogate are `InvalidRequest`, and `ProfileInvariants` rejects such stored strings, because UTF-8/JSON encoding replaces them with U+FFFD: the id was not stored verbatim and a transaction id no longer matched after a restart (a replayed reward applied twice).
13. **Reward items are snapshotted** into an ordinal immutable dictionary when the call is made; shape check, fingerprint and application use that copy, so a caller mutating its dictionary while the request waits cannot bypass validation. A caller dictionary whose comparer admits two ordinally equal keys is `InvalidRequest`.
14. **`InMemoryProfileRepository`** stores profiles in a `ConcurrentDictionary` (different ids run in parallel under different locks; the plain `Dictionary` lost entries). `TransactionProcessor` requires `IdempotencyWindow >= 1` (0 silently disabled duplicate detection).

Known limits (recorded, not fixed): (a) revisions are the only CAS token, so if a primary is lost and recovered from the backup while a second, stale processor in the same process still holds the higher revision, that processor's next save can pass the CAS over a newer commit (requires on-disk corruption plus two live processors on one repository; cross-process sharing is already out of scope). (b) The temp file is flushed with write-through, but `File.Replace`/`File.Move` renames are not themselves flushed, so after a power loss (not a process crash) the last acknowledged rename may be rolled back by the file system; the previous generation is then loaded.

Not implemented (non-goals unchanged): cross-process sharing, PostgreSQL, original price/cargo rule integration, upgrade eligibility and retained-state-on-death rules. A later additive [RT-F068 binding](../development-log/cloud/cloud-rt-f068-progression-contract.md) provides `OriginalProgressionIds` plus `ProfileContentValidator` and a reconstructed original-ID save/load fixture. Repositories/processor do not invoke that validator automatically; it does not establish native persistence or the M2 loop.
