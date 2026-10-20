Review status: APPROVED (senior review round 1, head 4953f8f; 0 blocker, 0 major, 1 minor, 1 nit)

# cloud/rt-f042-mineral-spawns — RT-F042 mineral spawn data audit

-   RT-ID: RT-F042 (gate items 2/5). Base: origin/main, not stacked. Status: done for audit scope; RT-F042 stays UNCHECKED.

## Done (`python scripts/audit-mineral-spawns.py <assets>` -> `MINERAL_SPAWNS_OK 42 type5 rows 12 mineral rows ...`)

-   LIO/Rogue.lio: 81 rows, 42 type5 v23 rows with 28 fields each. 12 carry a positive item id in f[20]: Tellerium 2003 rows 15,16,25,26,50,51,54; Krandazite 2005 rows 17,53; Iron Ore 2019 rows 77,78,79. All ids resolve to Resources in ITM/Rogue_mp.itm. Tail f[26]/f[27] = 350 with 15 (10 for Krandazite).
-   Raw timing pairs f[11]/f[12] (row15 3000/6000, row16 4500/4500, row26 3500/8500), f[14] in {0,40,60,80}, row54 count -1/-1 are recorded, not interpreted.
-   Negative f[20] rows are actors (MeteorShower row52 = -402), not minerals.
-   CFG/rogue.cfg row35 ItemPickupDistance=80; NWS/Rogue.nws row470 "Adjusted mineral spawns. A mineral spawn anchor has a detection radius ..." pinned.

## Remaining

-   Meaning of f[11]/f[12], f[14] (likely the player-exclusion radius, unproven), f[15..17], f[18]/f[19] counts, tail 350/15 and random placement in the area; trap rows; simulation and native verification. No C# added (no .NET SDK in sandbox): not verified in cloud - local CI required.

## Resume point

Typed LIO type5 reader (G6 is claimed by claim/g6-level-reader; stack on it when merged) and native evidence for field meaning.
