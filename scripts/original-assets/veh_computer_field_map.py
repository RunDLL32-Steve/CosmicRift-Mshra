#!/usr/bin/env python3
"""Field-index map of VEH v53 type-5 (Computer) rows, transcribed from the order of reads in the
InfantryOnline server parser (Infantry-Online-Server bb97579e dotnetcore/Assets/Veh/VehInfo.cs,
VehInfo.Computer.cs, VehInfo.ComputerProduct.cs, Shared/AnimatedSprite.cs, SpriteBlob.cs, HSV.cs,
ArmorValues.cs). This is an upstream-emulator interpretation of the field ORDER, not original
executable evidence. It prints index -> name and checks the map against the pinned rows.

Usage: veh_computer_field_map.py [VEH/Rogue.veh]   print field counts of the type-5 rows, then index -> name
       veh_computer_field_map.py --check VEH/Rogue.veh   exit non-zero unless every type-5 row has exactly 477 fields, every
           string slot (see is_string_slot) is non-numeric, empty/None or a quoted string
           (row 402's Description is the quoted text "0"), and every other slot parses as an integer.
Standard library only. Every name is an upstream-parser field name (InfantryOnline bb97579e), not original-executable evidence.
The pinned file is read, never written."""
import re, sys
def sprite_v3(p): return [p+'.BlobName', p+'.BlobId', p+'.LightPermutation', p+'.PaletteOffset', p+'.Hue', p+'.Saturation', p+'.Value', p+'.AnimationTime']
def anim(p): return [p+'.TickDistance', p+'.TickFrequency', p+'.Speed', p+'.Color'] + sprite_v3(p+'.Blob')
names = ['Type', 'Version', 'Id', 'Name', 'SkillLogic', 'Description', 'Hitpoints', 'Weight', 'ClassId']
for i in range(6): names += [f'Armor{i}.SelfIgnore', f'Armor{i}.PassIgnore', f'Armor{i}.SelfReduction', f'Armor{i}.Pass']
names += ['BarrelLength', 'FireHeight', 'PhysicalRadius', 'TriggerRadius', 'VehiclePhysicsOwned', 'VehiclePhysicsUnowned', 'WeaponPhysicsOwned', 'WeaponPhysicsUnowned', 'LowZ', 'HighZ', 'PickupItemId', 'IsControllable', 'IsWarpable', 'EnergyCostPerTick', 'NormalWeight', 'StopWeight', 'DisplayHealth']
names += [f'InventoryItems{i}' for i in range(6)] + [f'ChildVehicles{i}' for i in range(8)]
names += ['ExplodeItemId', 'RadarPrize', 'VehicleGetInMode', 'VisualHeight', 'DropItemId', 'DropItemQuantity']
names += anim('Sprites0') + anim('Sprites1')
names += ['EnergyMax', 'EnergyRate', 'EnergyMin', 'ThrowTime', 'AllowWeapons', 'Style1BallSpeed', 'Style1LowFireAngle', 'Style1HighFireAngle', 'Style1KeyAssignment', 'Style1BallFriction', 'Style2BallSpeed', 'Style2LowFireAngle', 'Style2HighFireAngle', 'Style2KeyAssignment', 'Style2BallFriction']
names += [f'TerrainModifiers{i}' for i in range(16)]
names += ['BallCarryRollVehicleItem', 'BallCarryArmorVehicleItem', 'FlagCarryRollVehicleItem', 'FlagCarryArmorVehicleItem', 'EventString1', 'EventString2', 'EventString3', 'LosDistance', 'LosAngle', 'LosXRay', 'CombatAwarenessTime', 'SiblingKillsShared', 'RelativeId', 'DisplayOnFriendlyRadar', 'FriendlyRadarColor', 'DisplayOnEnemyRadar', 'EnemyRadarColor']
for i in range(30): names += [f'HoldItemLimits{i}', f'HoldItemExtendedLimits{i}']
base_end = len(names)
names += ['RotateSpeed', 'AngleStart', 'AngleLength', 'TrackingTime', 'TrackingRadius', 'TrackingWeightLow', 'TrackingWeightHigh', 'FireRadius', 'RepairRate', 'HitpointsRequiredToOperate', 'RemoveGlobalTimer', 'RandomizeAim', 'ObeyLos', 'ComputerEnergyMax', 'ComputerEnergyRate', 'Destroyable', 'DensityRadius', 'DensityMaxType', 'DensityMaxActive', 'DensityMaxInactive', 'MaxTypeByPlayerRegardlessOfTeam', 'FrequencyMaxType', 'FrequencyMaxActive', 'FrequencyMaxInactive', 'FrequencyDensityMaxType', 'FrequencyDensityMaxActive', 'FrequencyDensityMaxInactive', 'ChainedTurretRequiredForBuilding', 'ChainedTurretRequiredForOperation', 'ChainedTurretId', 'ChainedTurretRadius', 'NumChainedTurretsRequired', 'LogicTakeOwnership', 'LogicStealOwnership', 'SpriteTurretZAdjust']
names += sprite_v3('SpriteBase') + sprite_v3('SpriteTurret')
prod_start = len(names)
for k in range(16): names += [f'Product{k}.{n}' for n in ['Title', 'ProductToCreate', 'Quantity', 'Team', 'Time', 'PlayerItemNeededId', 'PlayerItemNeededQuantity', 'Cost', 'SkillLogic', 'TeamItemNeededId', 'TeamItemNeededQuantity', 'TeamBuildingLogic', 'TeamQueueRequest', 'Confirm']]

def is_string_slot(name):
    """String-typed slots in the upstream parser: names, logic expressions, descriptions, event strings, blob refs, product titles."""
    return (name in ('Name', 'SkillLogic', 'Description', 'EventString1', 'EventString2', 'EventString3', 'LogicTakeOwnership', 'LogicStealOwnership')
            or name.endswith('.BlobName') or name.endswith('.BlobId')
            or re.fullmatch(r'Product\d+\.(Title|SkillLogic|TeamBuildingLogic)', name) is not None)

def split_line(line):
    """Same rules as the C# RawRecordReader: padding trimmed, a field starting with a quote runs to the next quote. Returns [(value, quoted)]."""
    out, pos = [], 0
    while True:
        p = pos
        while p < len(line) and line[p] in ' \t': p += 1
        if p < len(line) and line[p] == '"':
            close = line.index('"', p + 1)
            out.append((line[p + 1:close], True)); end = close + 1
            while end < len(line) and line[end] in ' \t': end += 1
        else:
            end = line.find(',', p)
            if end < 0: end = len(line)
            out.append((line[pos:end].strip(' \t'), False))
        if end >= len(line): return out
        pos = end + 1

def type5_rows(path):
    """Type-5 rows as lists of (value, quoted); blank lines are skipped."""
    rows = [split_line(l) for l in open(path, encoding='latin-1', newline='').read().replace('\r\n', '\n').replace('\r', '\n').split('\n') if l.strip(' \t')]
    return [r for r in rows if r[0][0] == '5']

INT_RE = re.compile(r'^[+-]?\d+$')

def check(path):
    problems = []
    rows = type5_rows(path)
    if len(rows) != 17: problems.append(f'expected 17 type-5 rows, found {len(rows)}')
    for r in rows:
        rid = r[2][0] if len(r) > 2 else '?'
        if len(r) != len(names):
            problems.append(f'row id {rid}: {len(r)} fields, expected {len(names)}'); continue
        for i, n in enumerate(names):
            v, quoted = r[i]
            if i == 1:
                if v != 'v53': problems.append(f'row id {rid}: f[1]={v!r}, expected v53')
            elif is_string_slot(n):
                if v and v != 'None' and not quoted and INT_RE.match(v): problems.append(f'row id {rid}: string slot f[{i}] {n} is an unquoted number ({v!r})')
            elif not INT_RE.match(v):
                problems.append(f'row id {rid}: numeric slot f[{i}] {n} is not an integer ({v!r})')
    n_str = sum(1 for n in names if is_string_slot(n))
    print(f'CHECK type-5 rows={len(rows)} fields-per-row={len(names)} string-slots={n_str} integer-slots={len(names) - 1 - n_str} problems={len(problems)}')
    for p in problems[:40]: print('  ' + p)
    print('CHECK ' + ('PASSED' if not problems else 'FAILED'))
    return 0 if not problems else 1

if __name__ == '__main__':
    args = sys.argv[1:]
    if args and args[0] == '--check':
        if len(args) != 2: sys.exit('usage: veh_computer_field_map.py --check VEH/Rogue.veh')
        sys.exit(check(args[1]))
    print(f'# base fields end at f[{base_end-1}], computer fields start f[{base_end}], products start f[{prod_start}], total {len(names)}')
    if args:
        for r in type5_rows(args[0]):
            print(f'row id {r[2][0]} {r[3][0]!r}: fields={len(r)} expected={len(names)}')
    for i, n in enumerate(names): print(f'f[{i}] {n}')
