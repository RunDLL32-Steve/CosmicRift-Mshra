# M2 cargo capacity and collectible pickup — October 16, 2026

Branch `feat/m2-cargo-pickup` (from origin/main 3452d48), queue item M2. Scope: generic pure-C# engine in `server/RogueTrader.Simulation`; no client, persistence-processor or original-spawn wiring.

-   Reused: `ICargoPolicy` contract and string-keyed inventory from `server/RogueTrader.Persistence` (the engine accepts a persisted inventory as the initial hold and `CargoCatalog.Fits` answers the same question), `CircleCollision.Sweep` for pickup, the `GameData`-style JSON loading options, and `OriginalItemCatalog` masses (test only).
-   Rules: one hard weight capacity; add takes `min(requested, free weight / unit weight, int range)`; the remainder stays in space; an initial inventory above capacity accepts nothing and free weight never goes negative. Weight 0 items are allowed.
-   NEW/provisional: every capacity, quantity, radius and fixture weight in the tests; pickup reach = ship radius + pile radius (the native CFG pickup value 80 is unvalidated).
-   Not claimed: native per-hull threshold/overload formula (RT-F030), LIO generation/trap fields (RT-F042), wreckage drops (RT-F043), client HUD (queue item M8).
-   Verified: `dotnet test tests/Unit` 520 passed, 5 skipped (pre-existing OriginalKeyListingTests, unrelated), 0 failed; 19 new cargo cases all passed including the original-asset case. Skipped: Godot smoke, derived-source and native checks (nothing there changed).
