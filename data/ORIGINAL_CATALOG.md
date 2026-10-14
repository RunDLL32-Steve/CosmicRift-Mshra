# Canonical Rogue Trader content catalog

Master authority: [§1.2, §§6–8 and M2](../ROGUE_TRADER_2_PLAN.md). Reconcile with [the data plan](IMPLEMENTATION_PLAN.md) and [PROGRESS.md](../docs/development-log/PROGRESS.md).

Read from unchanged `fowlmouth/cosmic-rift-assets` commit `eb1514eaae7f9b9425307d9cc043c74ecdd15e35`. The source index retains every raw field as a string; no original value is normalized or converted to invented units. Source hashes are verified before/after reading. Recreate with `python scripts/index-original-content.py`; generated output is outside originals at `artifacts/research/original-content/index.json`.

## Selectable hull records

Twelve `RPG/Rogue.rpg` v6 entries match candidate vehicle IDs and names in `VEH/Rogue.veh` v53. This is an identity cross-match, not validation of installation eligibility, physics scaling or runtime behavior.

| Original name | RPG identifier | Candidate vehicle identifier |
|---|---|---|
| Asp | 2 | 2002 |
| Excelsior | 15 | 2011 |
| Ravager | 16 | 2012 |
| Hornet | 8 | 2000 |
| Skithist | 1 | 2001 |
| Mastakith | 4 | 2004 |
| Tarthist | 11 | 2010 |
| Kragath | 3 | 2003 |
| Rak'Kir | 5 | 2005 |
| Rip'Pir | 6 | 2006 |
| Zap'Pir | 9 | 2008 |
| Zag'Gir | 10 | 2009 |
## Original resource records

Seven original type-4 resource records in `ITM/Rogue_mp.itm` v57. Ammunition, credits, repair records and projectile effects retain their separate categories.

| Original name | Item identifier | Source row |
|---|---|---|
| Tellerium | 2003 | 68 |
| Refined Tellerium | 2004 | 69 |
| Krandazite | 2005 | 70 |
| Refined Krandazite | 2006 | 71 |
| Wreckage | 2008 | 87 |
| Iron Ore | 2019 | 166 |
| Alloy | 2020 | 167 |
The full index has 3,086 records (CFG 2,763, VEH 30, ITM 200, RPG 12, LIO 81), 112 weapon/effect candidates and 551 filename/stem references. It deliberately reports 545 unresolved archive-member candidates; matching filenames is not complete BLO object decoding. Preserve all original fields and names until a version-aware engine/schema mapping is validated. The current Interceptor/plasma/training JSON is a temporary engineering fixture and is not this catalog.
