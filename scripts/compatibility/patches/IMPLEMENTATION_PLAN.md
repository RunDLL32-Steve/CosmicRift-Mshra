# Derived compatibility transformations

Purpose: narrow asserted transformations of unchanged pinned InfantryOnline source into ignored working copies. Parent: [compatibility plan](../IMPLEMENTATION_PLAN.md); master [§18 fidelity gate RT-F388/RT-F390](../../../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype).

Current stage, October 14, 2026: **Complete for scoped RT-F388/RT-F390 fixes.** [shop-sales.ps1](shop-sales.ps1) exports the sell validation/cash-ordering transform. It rejects zero/oversized sales and arithmetic overflow, revalidates ownership/cash after script hooks, removes inventory successfully before crediting, then synchronizes committed inventory/cash. [null-script-capacity.ps1](null-script-capacity.ps1) exports four null-safe script comparisons while retaining original thresholds/messages and case-insensitive sport exceptions. The parent preparation script composes both transformations and records original/derived SHA-256 once for ScriptArena.cs. Both reject unexpected/already patched source; the session-host wrapper requires that provenance before launch.

-   [x] Verify original defect reproduction and completed fixes through [RuntimeChecks](../RuntimeChecks/IMPLEMENTATION_PLAN.md): 17 actual sell-handler checks and 12 capacity-boundary checks pass. With six routing checks, the initial historical suite had 35 checks and the retained [baseline](../../../artifacts/rogue-runtime-checks/baseline.json) exposed 16 expected failures. The current [fixed report](../../../artifacts/rogue-runtime-checks/fixed.json) additionally covers buying/store access and passes 61 checks.
-   [x] Historical [transformation guards](../../../artifacts/rogue-runtime-checks/transformation-guards.json) confirm that the sale/capacity transforms left buying unchanged, rejected repeated application and required fresh compilation. RT-F396 later changes buying through its separate transform; the old unchanged-buy claim is scoped to the earlier transforms.

## Next and parity

-   [x] RT-F389 [shop-store-access.ps1](shop-store-access.ps1): packet shop transactions require the original /buy,/sell location rule (StoreEnabled terrain, or spectator with SpectatorStore); fails closed on missing state/out-of-range positions; guard runs before any price/inventory/cash/script side effect. 11 Store.\* checks. Not covered: skill shop (terrain skillEnabled), durable transactions, native store UI.
-   [x] RT-F396 [shop-purchases.ps1](shop-purchases.ps1): 64-bit price math, negative-price and 65535-stack rejection, post-hook revalidation, exact delivery with rollback of partial grants, debit only after delivery (Upgrade/Skill/Multi items debit first and refund if refused). Verified by 15 Buy.\* checks in ShopChecks.cs. Not covered: store eligibility (RT-F389), durable transactions, multi-item cash side effects.
-   [ ] Durable transaction atomicity, native trading and persistence require separate acceptance; these transformations establish no full original economy parity.

No canonical files or pinned upstream references are modified. The existing [license audit](../../../docs/original-research/INFANTRYONLINE.md) establishes no repository-wide redistribution grant; transformed upstream source remains in ignored local compatibility storage. Original native behavior and production server acceptance remain open.

# Derived packet-length transform

Master: [§18 RT-F397](../../../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype). Parent: [compatibility plan](../IMPLEMENTATION_PLAN.md).

-   [x] Complete outgoing length/queue scope — RT-F397 removes exactly two outgoing byte-array ushort casts. Original receive constructor and serialized bytes remain unchanged. `prepare-derived-source.ps1` reapplies from pristine pinned source and records both hashes; the fixed wrapper verifies the exact transform.
        Historical baseline/fixed checks pass 46 canonical packets and 60 inert queue fixtures; RT-F397 is merged to main (8b34c38); see [issue log](../../../docs/development-log/2026-10-14-rt-f397.md). Native delivery and other pending branches are outside this fix.
