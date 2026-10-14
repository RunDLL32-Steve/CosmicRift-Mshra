# Data implementation plan — M0 / M1

Status authority: [master M0](../ROGUE_TRADER_2_PLAN.md#milestone-0--repository-and-historical-research), [master M1](../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype) and [First Contact](../ROGUE_TRADER_2_PLAN.md#19-first-playable-build--version-01). Reconcile with [project progress](../docs/development-log/PROGRESS.md).

- [x] `ships/starter_interceptor.json`: one playable provisional Interceptor, equal forward/reverse thrust, mass, radius, speed cap, rotation, defenses and reactor.
- [x] `weapons/plasma_cannon.json`: one provisional weapon with speed, radius, damage, cooldown, energy cost, lifetime and velocity-inheritance switch.
- [x] `sectors/first_contact.json`: one sector, safe launch point, three stationary targets and 12 spaced static asteroids, with an unobstructed initial target lane.
- [x] Record exact chosen values in [balance documentation](../docs/balance/PROVISIONAL_VALUES.md).
- [x] Validate real JSON loads, NEW classifications and nonoverlapping entity placement in the unit suite; the initial target is destructible with the starting loadout.

The JSON and C# defaults are NEW/provisional design choices. Equal reverse thrust and ship-velocity inheritance are inspired by COMMUNITY evidence; these numbers are not recovered historical statistics. Future mineral, upgrade and additional ship content remains deferred to M2.

Evidence: 41 unit cases passed, zero failed/skipped, SDK 8.0.419, during the October 12, 2026 implementation session (America/Chicago). Final exported client verification is recorded in the master/client subplan.


- [x] Index the unchanged original source in a separate generated area and record canonical hull/resource identities in ORIGINAL_CATALOG.md.
- [ ] Replace engineering JSON fixtures with a validated original engine/data mapping after the compatibility decision; never overwrite canonical sources.
