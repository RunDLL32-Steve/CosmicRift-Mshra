Review status: review-pending

# Armour and damage types (RT-F041) — November 8, 2026

Branch: `claude/project-thread-50hrot`, based on `75747e6` (`origin/main`). Scope authority: [RT-F041](../original-research/FEATURE_CATALOG.md#rt-f041), [master §18](../../ROGUE_TRADER_2_PLAN.md#18-development-roadmap). Picked from the dashboard's "What's next" list (armour and damage types are not modelled yet).

## Evidence

-   **Where the protection grades are stored.** The nine Hull Armor rows are ITM type 13, which the InfantryOnline emulator (`InfantryOnline/Infantry-Online-Server` at `bb97579`) parses as a utility item: `Docs/Itm/Utility.txt` elements 53 to 64 and `dotnetcore/Assets/Itm/ItmInfo.UtilityItem.cs` (`values[52]` to `values[63]`) are six (Ignore, Percent) pairs for Kinetic, Explosive, Electronic, Psionic, Bypass and Energy damage. The same file names f[42..44] Vehicle Top Speed, Vehicle Hyper Top Speed and Vehicle Thrust, which explains the shared block the RT-F041 data review found (and refutes it being protection).
-   **The pinned values match every help text.** Raw f[52..57] (Kinetic, Explosive, Electronic):

| Armour                | Kinetic  | Explosive | Electronic (Energy) | Help text                                     |
| --------------------- | -------- | --------- | ------------------- | --------------------------------------------- |
| Duraplas Armor        | 0, 45    | 0, 25     | 0, 15               | medium / light / very light                   |
| Carbolux Armor        | 1000, 90 | 0, 50     | 0, 30               |                                               |
| Maxilite Armor        | 1000, 40 | 0, 20     | 0, 10               |                                               |
| Sillicate Armor       | 1000, 50 | 1000, 50  | 0, 0                | strong / medium / **no** protection vs Energy |
| Tiathamate Armor      | 2000,100 | 1000, 100 | 0, 40               |                                               |
| Bio Carapace          | 1000, 90 | 0, 10     | 0, 80               | strong / light / medium                       |
| Enhanced Bio Carapace | 1000,100 | 0, 45     | 0, 100              |                                               |
| Ablative Shell        | 0, 0     | 0, 45     | 1000, 70            | **no** protection vs Kinetic / light / good   |
| Absorption Shell      | 0, 0     | 0, 60     | 2000, 120           | none / light / excellent                      |

-   Psionic, Bypass and Energy (shield drain) pairs, f[58..63], are 0 in every armour, and f[52..63] are 0 in every other type-13 row. The twelve hulls' own VEH armour (f[9..32]) is 0.
-   **Damage types.** NWS/Rogue.nws row 133: "All weapons will do one of 3 types of Hull Point Damage: Kinetic, Explosive or Energy. You can purchase armor that has a protective effect against one or 2 of these damage types, but not all 3." Each weapon row carries exactly one of the kinetic (f[152]), explosive (f[156]) or electronic (f[160]) hull damages; the help text's "Energy" is the emulator's Electronic. The three starting cannons are Energy, Mass Drivers and chunks Kinetic, Moltor/missile weapons Explosive.
-   **Order and units.** The emulator applies vehicle armour as `(gross - SelfIgnore) * (1 - SelfReduction / 1000)` (`ZoneServer/Game/Objects/Vehicle.cs`); it never applies the utility items' pairs. This change applies the armour's pairs in that same order. Ignore is in the thousandths that hull damage uses (1000 = one hull point). Percent is read as per mille, the vehicle SelfReduction scale; it is the only reading under which every pinned value stays below 100% (`chaosadvanced.itm` carries 990). Under it, Absorption Shell takes a Plasma Cannon hit from 5 to (5 - 2) \* 0.88 = 2.64.

## Change

-   `DamageType` (Kinetic, Explosive, Energy) on `WeaponDefinition`; `OriginalWeaponBuilder` sets it from the weapon's damage field. Regenerated `data/weapons/original_*.json`.
-   `ShipArmor` on `ShipDefense` (and on practice `TargetDefinition`s); `WorldSimulation.ApplyDamage` passes the hull damage through `ShipArmor.HullDamage` before the shield rule. Armour never heals and never adds damage; the shield drain is untouched.
-   `OriginalArmor` exposes the raw pairs; `OriginalStationBuilder` gives each armour its protection and drops the invented "buy / 200 hull points" bonus. `UpgradeEffects` adds the active armour (one per held category, as before) to the hull's own. Regenerated `data/stations/trading_outpost.json`.
-   The station's upgrade list shows each armour's protection (for example `Kin 2+10% Exp 1+10% En 4%`), with the full effect in a tooltip.

## Still open (RT-F041 stays unchecked)

-   Percent as per mille is a reading, not a native measurement. A local check against the original client would settle it.
-   Remove Armor (ITM id 41, buy 0) at a Repair Dock with a confirmation (NWS row 323) is not built: it needs a new profile transaction.
-   f[42..44] still feed forward thrust / maximum speed / rotation, but the emulator names them top speed / hyper top speed / thrust. Fixing that changes every engine and thruster mod, so it is left for an RT-F048 change.
-   Blast radius and outer damage are not applied (projectiles hit at the inner value).

## Checks

`dotnet test tests/Unit` with the pinned assets: 1,230 passed, 13 skipped, 1 failed (`BloCanonicalTests`, the BLO files are Git LFS pointers in this cloud checkout; unrelated to this change). Integration 6/6.

Cloud run (apt SDK 8.0.133 with `global.json` set aside and restored, Godot 4.5.1 mono headless): client build 0 errors, Godot import exit 0, `--smoke-test` exit 0 with every INPUT, STATION and flight/combat SMOKE PASS line. `python scripts/format-check.py` FORMAT_CLEAN and `python scripts/check-trackers.py` TRACKERS_CONSISTENT (397/22/375). Not run in cloud: `scripts/verify.ps1` with the pinned SDK 8.0.419 on Windows.
