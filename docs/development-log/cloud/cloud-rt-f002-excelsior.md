Review status: UNREVIEWED

# cloud/rt-f002-excelsior

- Claims: RT-F002 (Excelsior hull, HULL profile; data/reader clauses only). No TASK_QUEUE row exists for RT-F002 (nothing to mark CLAIMED).
- Stacked on: none (cut from origin/main 1ce1d21). The local branch cloud/rt-f001-asp-hull-oracle carries no commits of its own (it points at an old main commit, has no remote, no PR); there is no F001 loader to reuse beyond the generic OriginalHullCatalog already on main.
- Status: wip.

## Acceptance clauses (HULL profile)
Planned (data): per-hull oracle test pinning the Excelsior record exactly as catalogued (RPG row 2, skill 15, vehicle 2011, VEH row 28, hull f[6]=50, cargo f[47..48], speed f[202..211], roll TerranShipsSmall/Excelsior).
Remaining (not claimed): sprite/effect/sound resolution, directional rendering, energy/movement/terrain/installation/loadout validation against behavior, native hull acceptance.

## Resume point
Write the oracle test and plan.d entry, run dotnet test tests/Unit and check-trackers, BBQ commit, open PR.
