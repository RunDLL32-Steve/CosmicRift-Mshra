# Original radar look (RT-F064 radar rendering step)

Authority: [master §16 appearance](../../ROGUE_TRADER_2_PLAN.md#16-graphical-direction) and the RT-F064/RT-F025 catalog rows. This step draws the radar with original values; RT-F064 and RT-F025 stay open (397 boxes unchanged).

-   [x] Draw the player in CFG [Radar] ColorSelf and practice targets in ColorEnemy from `data/ui/radar.json`, checked against the pinned `CFG/rogue.cfg` lines 2278-2280.
-   [x] Stage the [Uiart] RadarBorder art (`cruiart,newradar`, line 2751) with `stage_ui_art.py` and draw it around the radar (RECONSTRUCTED piece layout); keep the NEW frame when it is not staged.
-   [ ] Native radar range, zoom, blip shapes, FontColorSelf/Friendly/Enemy use, friendly ships, team vision sharing and line-of-sight rules.
-   [ ] Native comparison of the frame layout and pieces 4 and 7.
