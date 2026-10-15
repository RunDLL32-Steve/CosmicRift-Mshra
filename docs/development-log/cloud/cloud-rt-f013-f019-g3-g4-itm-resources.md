Review status: UNREVIEWED

# cloud/rt-f013-f019-g3-g4-itm-resources

- Claims: RT-F013..RT-F019 (data clauses only), queue G3 and G4, gate items 2 and 5 (data). Base: origin/main 2932fe2. Status: done for the data clauses; features stay UNCHECKED.
- Scope: raw-preserving loader for all 200 ITM rows (`OriginalItemCatalog`, `OriginalItem`, `ItmFields`) and the seven type-4 `Resources` rows with typed mass f[7], buy f[8], sell f[15] accessors. The sell -1 is flagged as a negative-sentinel candidate and never replaced.
- Files: shared/RogueTrader.OriginalData/OriginalItem.cs, OriginalItemCatalog.cs; tests/Unit/OriginalItemCatalogTests.cs; scripts/audit-itm-resources.py; TASK_QUEUE.md.

## Acceptance clauses
Done (data): ids, names, rows, mass, buy and sell of all seven rows match the catalog; all 200 rows are v57 with unique ids and at least 31 fields.
Remaining (not done, so no checkbox ticked): operational interpretation of mass/sell sentinels, pickup/cargo behaviour, native presentation, refinery use (RT-F044), and sprite/art identity.

## Verified
- `python3 -I scripts/audit-itm-resources.py <pinned assets clone>` printed `ITM_RESOURCES_OK 7` (pinned length and SHA-256 matched; 200 rows, v57, unique ids; seven rows equal the oracle). Assets cloned from fowlmouth/cosmic-rift-assets at eb1514ea.

## Not verified in cloud - local CI required
- The C# code and tests were NOT compiled or run: .NET 8 SDK downloads are blocked by the proxy (403 and refused hosts) in this sandbox. Run `dotnet test tests/Unit` locally.
- check-trackers result is below if it was run.

## Resume point
Local: build and run `dotnet test tests/Unit`, then tick RT-F013..F019 data sub-items only if the owner accepts them as scoped completions. Next cloud item: G5 (weapon/ammo records) or G7, building on this catalog.
