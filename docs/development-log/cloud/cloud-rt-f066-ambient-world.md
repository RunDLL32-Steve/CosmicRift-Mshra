Review status: UNREVIEWED

# cloud/rt-f066-ambient-world — RT-F066 ambient/decorative world data audit

- RT-ID: RT-F066 (gate items 2/5). Base: origin/main, not stacked. Status: done for audit scope; RT-F066 stays UNCHECKED.

## Done (`python scripts/audit-ambient-world.py <assets>` -> `AMBIENT_WORLD_OK 5 sprites ...`)
- LIO/Rogue.lio rows 1-5: type9 v23 (21 fields) Jupiter (7824,5504, Planets/jupiter), Mars (7408,10848), LNebula2 (12077,8726, BackgroundSprites/Lnebula2), Moon1 (15376,5056), Moon0 (8688,7168); f[10] 35/35/30/35/40; f[19] 0/-5/-20/-2/-7 kept raw.
- Rows 35 Scorpius Nebula (16432,1888,2000x1000) and 59 Marianas FreePort (13696,8768,1500x1500) are type8 with description text.
- Rows 63/64 AmbientA/AmbientB type7 at (27006,20942)/(26558,19438), sounds SFAmbient/space_radio_lp and ambience_lp.

## Remaining
- Resolving the Planets/BackgroundSprites blobs and SFAmbient audio (BLO is Git LFS, not read), crop/palette/depth/parallax, the meaning of f[10]/f[19], native comparison. No C# added: not verified in cloud - local CI required.

## Resume point
BLO member resolution for the five sprites and two sounds once C1 (BLO reader) is available.
