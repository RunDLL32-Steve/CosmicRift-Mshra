# Client assets implementation plan

Reviewed October 13, 2026. Master: [M0/M1 engineering](../../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype), [§16 original appearance](../../ROGUE_TRADER_2_PLAN.md#16-graphical-direction), [fidelity registry](../../ROGUE_TRADER_2_PLAN.md#original-feature-registry--october-14-checkpoint). Reconcile with the [client plan](../IMPLEMENTATION_PLAN.md) and [original importer plan](../../scripts/original-assets/IMPLEMENTATION_PLAN.md).

## Purpose and completed history

[icon.svg](icon.svg) is the newly authored provisional application icon referenced by project.godot. It is not original Cosmic Rift artwork. Its .import sidecar is Godot resource metadata.

Canonical original source files remain unchanged in the pinned reference checkout. Derived PNG atlases, runtime.json and Rogue.png are staged separately in ignored client/OriginalContent, not this folder. Selected 11-archive/231-object/36,516-frame decoding and staging passed scoped RT-F070–RT-F072; viewer/map/package checks passed RT-F020/F073/F373. [Manifest provenance](../../docs/original-research/ASSET_MANIFEST.json), [standalone resource proof](../../artifacts/qa/OriginalContent/standalone-sprites.log).

## Active outstanding work

-   [ ] **Investigating — RT-F069/F377–F379:** runtime dependency eligibility, Portal member, logo and numeric BallGraphic interpretations. All-archive inventory completion does not establish closure.
-   [ ] **Investigating — RT-F023–RT-F025/F074/F075:** native HSV/palette/shadow/blit, banking/timing, layers and HUD rules.
-   [ ] **Not started — native appearance/asset-delivery acceptance:** verified client receipt and original level/object/ship/effect rendering after arena entry.

Keep original bytes, basenames, fields and source hashes; derived outputs stay separate. Local source availability and the local inspection export do not establish redistribution rights. No new assets or verification runs were produced for this documentation review.
