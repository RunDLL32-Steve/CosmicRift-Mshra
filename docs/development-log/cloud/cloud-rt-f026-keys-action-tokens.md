Review status: UNREVIEWED

# cloud/rt-f026-keys-action-tokens

- Claims: RT-F026 (Original controls); gate items 2, 6. Not claimed: RT-F368 (keys.txt help link, CFG row 183).
- Base: origin/main ac41dc9 (claim commit d15d5bd).
- Status: done for the data-only slice; the RT-F026 box stays OPEN (acceptance needs native token -> binding resolution and per-action verification).

## Scope
Data only: a hash-guarded reader for the archived Rogue `keys.txt` action/substitution-token listing, plus raw text pins for the CFG F11 line and NWS Home line. No gameplay or input code. `keys.txt` is not in the pinned asset clone and is deliberately NOT committed (redistribution licence unknown); it comes from InfantryOnline/Zone-Assets commit 401633b9 (SOURCE_MANIFEST.json, blob e8dc82bb, 1942 bytes).

## Acceptance clauses (catalog RT-F026)
Done (data/evidence only):
- keys.txt provenance verified before parsing (length 1942, git blob SHA-1 e8dc82bb..., SHA-256 d004b97b...); 5 sections, 57 actions, 65 tokens, every action -> token list pinned (Rotate 0/1, Thrust 2/3, Hyper Thrust 8/9, Strafe 6/7, Light/Heavy Weapon 11/12, Mine 13, Repulsor 14, Team-Warp 15, ECM 16, Utility 17, Special 18-20, Extra 21-26, Help 66, Main Menu 65, Ship Selection 50, Spectator 49, Map 27, Store 51, Alt. Ship 32/33, Extend View 5/4 (In/Out), Message Scroll 30/31, Message Toggle 48, Player List 28/29, Select 52, Toggle 34, Macros 1-12 = 35-46, 13-24 = 53-64; token numbers 10 and 47 are unused).
- Raw colour codes kept, stripped labels, separators (" / ") and "(not used in all zones)" notes (Toggle Store, Enter/Exit Alt. Ship) preserved.
- CFG/rogue.cfg line 41 ([Arena] ExitSpectatorMessage "...Press f11.") and NWS/Rogue.nws line 108 ("...activate it (Press Home).") pinned via hash-guarded reads (NWS sha256 7fff837b... from ASSET_MANIFEST.json).

Remaining (not done):
- Resolve each RT action token to the actual default binding/modifier (native client; tokens are NOT literal keys, including strafe vs the generic official command table).
- Implement and verify each completed action in the client/simulation.

## Files touched
- shared/RogueTrader.OriginalData/OriginalKeyListing.cs (new: OriginalKeyListing, KeyListingSection, KeyListingAction, OriginalControlReferences)
- shared/RogueTrader.OriginalData/OriginalDataExceptions.cs (enum value AssetIntegrityFailure.GitBlobSha1Mismatch appended)
- shared/RogueTrader.OriginalData/plan.d/rt-f026.md
- tests/Unit/OriginalKeyListingTests.cs (new; includes RequiresRogueKeysTxtFact, skip unless RT_ROGUE_KEYS_TXT points to an existing file)
- scripts/original-assets/audit_rt_f026.py, scripts/original-assets/fetch_rogue_keys.py (new, stdlib only; fetch writes outside the repo only and verifies all three hashes)

## Verified (cloud)
- Independent audit `python3 -I scripts/original-assets/audit_rt_f026.py /tmp/claude-0/keys.txt <assets>` : provenance MATCH, lines=71, sections=5, actions=57, per-action tokens as pinned, CFG line 41 and NWS line 108 text printed.
- fetch_rogue_keys.py really downloaded from raw.githubusercontent.com into a temp folder and verified all hashes; refuses repo-internal folders and overwrites.
- Offline xunit-compatible shim, apt SDK 8.0.131 (not the pinned 8.0.419/real xunit): `OriginalKeyListing` filter without RT_ROGUE_KEYS_TXT: the 5 keys.txt oracle tests Skipped, the rest passed (the CFG/NWS oracle ran because the asset clone was auto-located); with RT_ROGUE_KEYS_TXT and RT_ORIGINAL_ASSETS_ROOT: 12/12 passed. Full suite without env: Passed 305, Failed 0, Skipped 5, Total 310; full suite with both env vars: Passed 310, Failed 0, Skipped 0, Total 310 (main baseline 298).
- `python3 scripts/check-trackers.py`: TRACKERS_CONSISTENT (397 entries, 22 complete, 375 open; unchanged - no boxes ticked).

## Not verified in cloud - local CI required
- Pinned SDK 8.0.419 build and a real xunit run (only the offline shim was run).
- Oracle tests need RT_ROGUE_KEYS_TXT (fetch_rogue_keys.py) in addition to the asset clone; in CI without them they are Skipped, never passed.
- Native client behaviour: token -> key/modifier mapping, strafe, F11/Home actions, any BLO/LVL-dependent check.

## Resume point
Next step needs the native client / compatibility layer: determine how the original client substitutes `~~<n>key` (default binding table, modifiers, shift distinguishing for macros 13-24) and implement + verify each action; then tick RT-F026 in the same branch that finishes it.
