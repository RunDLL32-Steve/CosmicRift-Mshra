Review status: APPROVED (round 1, senior review, local)

# cloud/rt-f068-progression-contract

-   RT-IDs: RT-F068 (evidence + reconstructed binding only). Gate item 7 (M2 loop); also touches 2/5 (raw preservation of CFG/NWS/RPG evidence). Base: origin/main 3452d48; claim commit b9e4890. No queue row.
-   Scope: (1) pin the original persistence/progression evidence in the canonical package (NWS 26-29 character wipe, NWS 399 same-race hull purchase, related CFG/RPG raw values, a keyword scan for save/persistence words) and state honestly that no historical save schema exists in it; (2) a RECONSTRUCTED (NEW) original-id binding for the data-agnostic persistence core, with save/load verification of a profile built from original ids. No wipe, exchange, cash, reset, price or cargo rule is implemented.
-   Field-name evidence: RPG f[38] "CashAdjustment" and the [Event] verb meanings are upstream-emulator names/interpretations (InfantryOnline bb97579e, via FIELD_INTERPRETATION.md and RT-F067), not original-executable evidence. Values were read from the pinned files at eb1514ea.

## Acceptance clauses

Catalog acceptance: "Locate original persistence evidence or document unavoidable reconstructed backend contract; verify completed save/load retains precisely chosen inventory, hull, cash and upgrades."

-   Done (evidence): `OriginalProgressionEvidence` pins NWS 26-29 verbatim under the undated heading line 25 `********~BGame Overview~B*********` (26 blank; 28 "...cannot be selected without wiping ones character using ?wipecharacter"); NWS 399 "Added the ability to purchase a new ship hull. The caveat is you can only change to a ship hull that is the same race as your existing ship hull." under line 391 `*******9/08/01`; NWS 218/220 spec-mode losses under line 215 `********3/12/03`. CFG: [StartGame] 2146-2151 all 0 (PrizeReset, VehicleReset, InitialHides, ResetInventory, ResetCharacter, ClearProjectiles); [Event] 2492 Hold1, 2493 Hold2 (wipeinv lists), 2497 SysopWipe=EventSelfWipe, 2498 SelfWipe=wipescore,EventFirstTimeInvSetup,EventFirstTimeSkillSetup, 2499 FirstTimeInvSetup=Wipeinv,setcash=0,vehicleEvent, 2502/2503 KilledByTeam/KilledByEnemy=reset,warp=0, 2504 EnterSpawnNoScore=wipescore,EventFirstTimeInvSetup, 2505 ChangeDefaultVehicle= (empty), 2508 FirstTimeSkillSetup=wipeskill,setexp=1; [Owner] 2216 Wipe=0, 2217 Profile=0; [PublicProfile] 86 DefaultVItemId=2000; [Stat] 2544-2547 TrackDaily/Weekly/Monthly/Yearly=1. RPG f[38]=3499 on all 12 hull rows.
-   Done (absence of schema): keyword scan (wipe, character, save, persist, profile, reset, account, login, logout; case-insensitive) over the five hash-verified files: RPG 0, VEH 0, ITM 0 lines; CFG 19 lines (85, 858, 864, 2146, 2147, 2149, 2150, 2216, 2217, 2492, 2493, 2497, 2498, 2499, 2502, 2503, 2504, 2508, 2534 - all config switches/event strings; 2534 `Saves=60` is a [SoccerMvp] scoring weight); NWS 4 lines (28, 56, 129, 171; only 28 is about progression). No CFG line contains `wipecharacter`. The package therefore holds policy text and server event/config strings but no save-record layout, field order or storage format: the backend contract is unavoidably reconstructed.
-   Done (reconstructed contract, NEW): `OriginalProgressionIds` - ids are invariant decimal strings of original numeric ids (only form accepted: "Asp", "02002", "+2002", " 2002", skill id "2" are rejected as hulls); hull = VEH vehicle id of the 12 RPG/VEH-linked hulls {2000..2006, 2008..2012}; item = any of the 200 ITM ids; upgrade = the 26 ITM ids of the RT-F048 families (Engine/Thruster/Power Mods, HyperDrive Systems); hull and item namespaces are separate (2011 = Excelsior hull and HyperCore item). `ProfileContentValidator` (server/RogueTrader.Persistence, data-agnostic, three predicates, additive, not called by repositories/processor) reports unknown hull/inventory/upgrade ids.
-   Done (save/load): profile `f068-asp` with hull 2002 Asp, inventory {1004 Plasma Cannon (the Asp VEH f[129] addinv item) x1, 2003 Tellerium x7}, upgrades {2 TurboBoosters}, wallet 12345, then revision 1 adding upgrade 2011 HyperCore and wallet 7345, created and saved through `FileProfileRepository`, reopened in a new repository instance and loaded: hull, wallet, inventory, upgrades (and every other profile member) equal; the loaded ids still validate and resolve to the same original names. Quantities, wallet and the 5000 delta are NEW fixture numbers (f[129] names no count; no starting-cash rule is proven).
-   Done (policies as evidence only): the validator accepts a different-race hull (Asp -> Skithist) and a profile with no hull; no wipe, exchange or cash reset is applied.
-   Remaining (box stays OPEN): native/historical save/load behaviour and ordering (not in the package; would need server/backend evidence); which of NWS 28 (wipe to change ship) and NWS 399 (same-race purchase) governs, and what a hull change keeps; semantics of wipeinv/wipescore/wipeskill/setcash/setexp/reset/vehicleEvent (RT-F067); starting cash (RPG f[38]=3499 vs FirstTimeInvSetup setcash=0); DefaultVItemId target; which cargo survives logout (NWS 218/220, Hold1/Hold2, held-category limits); wiring the validator and the original catalogs into the M2 "mine, sell, buy upgrade, exit and return" loop.

## Visible mismatches (kept raw, listed in `VisibleMismatches`, 7 notes)

-   NWS 27-29 (undated overview) vs NWS 399 (9/08/01): later policy unproven.
-   No CFG `wipecharacter`; SelfWipe is the closest raw string, link unproven.
-   ChangeDefaultVehicle is empty: no CFG event for the same-race hull purchase.
-   RPG f[38]=3499 on all 12 hulls vs FirstTimeInvSetup setcash=0: starting-cash order/meaning unproven.
-   DefaultVItemId=2000 is both ITM 2000 (Light Mass Driver Pellets) and VEH 2000 (Hornet).
-   [StartGame] switches and [Owner] Wipe/Profile: native effect unvalidated.
-   NWS 218/220 spec-mode losses: retained-on-logout set not decided.

## Files

-   shared/RogueTrader.OriginalData/OriginalProgression.cs (new: OriginalNwsEvidenceLine, OriginalKeywordHit, OriginalProgressionEvidence, OriginalProgressionIds)
-   server/RogueTrader.Persistence/ProfileContentField.cs, ProfileContentViolation.cs, ProfileContentValidator.cs (new, additive)
-   server/RogueTrader.Persistence/plan.d/rt-f068.md (new)
-   tests/Unit/OriginalProgressionTests.cs (new, 11 asset tests), tests/Unit/Persistence/ProfileContentValidatorTests.cs (new, 6 synthetic tests)
-   scripts/original-assets/audit_rt_f068.py (new, stdlib-only, read-only)
-   docs/development-log/cloud/cloud-rt-f068-progression-contract.md (this log)
    Existing persistence code and tests are unchanged.

## Results

-   NuGet is blocked in the cloud; tests ran with the xunit-compatible reflection shim (SDK 8.0.131; global.json not modified).
-   Filter `OriginalProgressionTests` + `ProfileContentValidatorTests` with assets: passed 17, failed 0, skipped 0. Without assets: passed 6, failed 0, skipped 11.
-   Full unit set with assets: passed 518, failed 0, skipped 5 (main baseline 501/0/5; the 5 skips are pre-existing).
-   A deliberate wrong expectation in the save/load test failed as expected (assertions execute), then was reverted.
-   `python3 -I scripts/original-assets/audit_rt_f068.py /tmp/rtassets/src` reproduces every pinned line, value, id set and keyword hit.
-   Existing persistence classes (FileProfileRepository, EconomyTransaction, ProfileIdentityAndEncoding, ProfileSerializer, TransactionConcurrency tests): passed 129, failed 0, skipped 0 (unchanged behaviour).
-   check-trackers: `checklist: 397 entries, 22 complete, 375 open` / TRACKERS_CONSISTENT (no tracker or box edited).
-   Not verified in cloud - local CI required: real xunit `dotnet test tests/Unit` with pinned SDK 8.0.419; `scripts/verify.ps1`.

## Status

done for the cloud-verifiable evidence/binding clause; feature box stays open. Resume point: obtain backend/native evidence for save order and the wipe vs exchange policy, decide event-verb semantics (RT-F067), then wire `ProfileContentValidator` + `OriginalProgressionIds` into the M2 loop and verify native save/load.
