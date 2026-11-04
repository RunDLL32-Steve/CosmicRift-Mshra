using System.Collections.Immutable;
using Godot;
using RogueTrader.Persistence;

namespace RogueTrader.Client;

public partial class GameClient
{
    // --verify-hull-sprites always selects a new disposable profile before this method runs.
    private void SeedHullSpriteCheckProfile()
    {
        if (_smokeProfileRoot is null || _profileRoot != _smokeProfileRoot)
            throw new InvalidOperationException("Hull sprite QA requires an isolated profile.");
        using var repository = FileProfileRepository.Open(_profileRoot, new PersistenceOptions(), ProfileSchemaMigrations.BuiltIn);
        var profile = PlayerProfile.CreateNew(LocalProfileId, 0, _roster.StarterHullId) with
        {
            OwnedShips = _shipCatalogs.Keys.ToImmutableSortedDictionary(id => id, _ => (ShipCondition?)null, StringComparer.Ordinal),
        };
        if (Await(repository.CreateAsync(profile)).Status != ProfileSaveStatus.Saved)
            throw new InvalidOperationException("Could not create hull sprite QA profile.");
    }

    /// <summary>Bounded presentation regression: no flight/combat simulation or real pilot save is used.</summary>
    private async void RunHullSpriteCheck()
    {
        try
        {
            _paused = true;
            var originals = _originals ?? throw new InvalidOperationException("Stage original hull atlases before hull sprite QA.");
            var paths = new HashSet<string>(StringComparer.Ordinal);
            foreach (var hull in originals.Hulls)
            {
                ShowShipPicker();
                RequireHullSprite(_shipPicker.Press(hull.VehicleId), $"Picker did not offer {hull.VehicleId}.");
                var path = await CheckDrawnHull(hull.VehicleId);
                RequireHullSprite(path is not null && paths.Add(path), $"Hull {hull.VehicleId} did not bind a distinct atlas.");
            }
            RequireHullSprite(paths.Count == 12, "All twelve canonical hull atlases must be drawn.");
            GD.Print("HULL_SPRITE_CHECK: all 12 canonical vehicle IDs selected through picker and bound to 12 distinct atlas textures.");

            // Reopen the services and saved active ship, then switch through the actual Hangar button signal.
            var savedHull = ActiveHullId;
            ReopenProfileFromDisk();
            await CheckDrawnHull(savedHull);
            var dock = new System.Numerics.Vector2(_stationDefinition.X, _stationDefinition.Y);
            _world.RestorePlayer(dock, _world.Player.Heading, _world.Player.Hull);
            SetStationVisible(true);
            RequireHullSprite(_stationVisible, "Hull QA must dock at the home store.");
            var hangarHull = originals.Hulls.First(h => h.VehicleId != savedHull).VehicleId;
            _stationScreen.FindChildren("*", "Button", true, false).Cast<Button>()
                .Single(button => !button.IsQueuedForDeletion() && button.HasMeta("station_action")
                    && button.GetMeta("station_action").AsString() == $"hangar:{hangarHull}")
                .EmitSignal(Button.SignalName.Pressed);
            await CheckDrawnHull(hangarHull);
            SetStationVisible(false);
            ReloadProfile();
            await CheckDrawnHull(hangarHull);
            ReopenProfileFromDisk();
            await CheckDrawnHull(hangarHull);
            GD.Print("HULL_SPRITE_CHECK: saved active hull, Hangar switch, reload and disk reopen retained the matching draw texture.");

            ShowShipPicker();
            RequireHullSprite(_shipPicker.Press("starter_interceptor"), "Legacy hull must remain selectable.");
            await CheckDrawnHull("starter_interceptor");
            RequireHullSprite(originals.HullSpriteForVehicle("unknown-hull") is null, "An unmapped ID must not select original artwork.");
            _view.Originals = null;
            ShowShipPicker();
            RequireHullSprite(_shipPicker.Press(originals.Hulls[0].VehicleId), "Canonical hull must remain selectable without originals.");
            await DrawHullFrame();
            RequireHullSprite(_view.PlayerAtlasPath is null, "Missing originals must retain the geometric fallback.");
            RequireHullSprite(_world.TickNumber == 0, "Hull QA must not advance gameplay ticks.");
            GD.Print("HULL_SPRITE_CHECK_PASS: legacy/unknown ID and absent originals use fallback; world ticks 0; isolated disposable profile.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"HULL_SPRITE_CHECK_FAILED: {exception.Message}");
            GetTree().Quit(1);
        }
    }

    private async Task<string?> CheckDrawnHull(string hullId)
    {
        RequireHullSprite(ActiveHullId == hullId && _world.Player.Definition.Id == hullId, $"Selected hull {hullId} did not reach the world.");
        var sprite = _originals!.HullSpriteForVehicle(hullId);
        var expectedPath = sprite is null ? null : $"res://OriginalContent/{sprite.AtlasPng}";
        await DrawHullFrame();
        RequireHullSprite(_view.PlayerAtlasPath == expectedPath,
            $"Hull {hullId} drew '{_view.PlayerAtlasPath ?? "fallback"}', expected '{expectedPath ?? "fallback"}'.");
        GD.Print($"HULL_SPRITE_DRAW: {hullId} => {_view.PlayerAtlasPath ?? "geometric fallback"}");
        return _view.PlayerAtlasPath;
    }

    private async Task DrawHullFrame()
    {
        await NextPhysicsFrame();
        _view.QueueRedraw();
        RenderingServer.ForceDraw();
    }

    private static void RequireHullSprite(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
