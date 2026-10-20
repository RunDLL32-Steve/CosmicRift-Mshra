# Repository automation implementation plan

Reviewed October 13, 2026. Master: [M0 automated workflow](../ROGUE_TRADER_2_PLAN.md#milestone-0--repository-and-historical-research), [M1 acceptance](../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype) and [§1.3 verification scope](../ROGUE_TRADER_2_PLAN.md#13-continued-exact-reproduction--october132026).

## Accomplished

[workflows](workflows/IMPLEMENTATION_PLAN.md) contains the Windows build definition for pinned bootstrap, provisional source/tests/smoke/export and artifact/TRX upload. This is an implemented workflow definition; a hosted successful run is unverified. Local historical prototype and original inspector acceptance are separate evidence.

## Active outstanding work

-   [ ] **Not started:** hosted workflow proof once a remote exists and the definition is pushed.
-   [ ] **Not started:** focused original importer/viewer/native compatibility coverage as appropriate; current build.yml invokes the default prototype verification/export modes.
-   [ ] **Investigating:** adjust CI scope to avoid baseline gameplay simulations for documentation-only or asset-only changes, while preserving required completed-feature acceptance.

Original sources remain unchanged and local canonical/native content rights are not inferred from repository access. No CI/build/test/simulation was run for this documentation review.
