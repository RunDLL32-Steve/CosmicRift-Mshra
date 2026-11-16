using System.Numerics;

namespace RogueTrader.Simulation;

/// <summary>
/// The 60-second dock rule (RT-F065 follow-up, Steve, Nov 16 2026: the original behaviour). CFG <c>MaxTimeAllowed</c> is 60 on
/// the Repair Docks (Terrain9) and the Refinery/Trade Zone (Terrain15); NWS line 483 (8/27/01) says a pilot "can only stay in the
/// safe zones protecting Refineries or Trading Posts for 60 seconds. After that you will be sent to spec."
/// <para>
/// The timing is the InfantryOnline emulator's (bb97579, <c>Arena.poll</c>; RECONSTRUCTED, no code copied): the clock is the
/// time since the pilot last moved (a change of the whole-pixel position) or fired, it is checked against the terrain under
/// the ship, a warning comes once the ship has stood still for half the limit, and a ship still for longer than the limit is
/// sent to spectator mode. A warp, a spawn and leaving spectator mode restart the clock. Shots refused in a safe zone never
/// leave the ship, so they do not count as firing (NEW).
/// </para>
/// <para>
/// The clock is real time (owner rules, Nov 1-2 2026): <see cref="ElapseWithoutTicking"/> adds the time the world is not
/// ticking (paused, docked at a station screen, choosing a ship, logged out), so none of those can stop it, and the caller
/// persists <see cref="PlayerStillTicks"/> with the ship so a relaunch cannot restart it either.
/// </para>
/// </summary>
public sealed partial class WorldSimulation
{
    /// <summary>Message the emulator sends at half the limit; {0} is the seconds left.</summary>
    public const string DockTimeWarningFormat = "WARNING! Staying in this location for another {0} seconds will send you to spectator mode.";

    /// <summary>Message the emulator sends when the limit sends a pilot to spectator mode.</summary>
    public const string SentToSpectatorMessage = "You have been sent to spectator mode due to inactivity.";

    private (int X, int Y) _stillPixel;

    /// <summary>Ticks since the player's ship last moved a whole pixel or fired (the emulator's <c>_lastMovement</c>).</summary>
    public long PlayerStillTicks { get; private set; }

    /// <summary>
    /// The pilot was sent to spectator mode: the ship is out of the game (it does not move, fire, recharge, collect or take
    /// damage) until <see cref="ReturnFromSpectator"/> brings it back.
    /// </summary>
    public bool PlayerIsSpectator { get; private set; }

    /// <summary>The half-time warning was given for the current still period (the emulator's <c>_maxTimeCalled</c>).</summary>
    public bool DockTimeWarningGiven { get; private set; }

    /// <summary>How many half-time warnings this world has given; a client shows one each time this goes up.</summary>
    public int DockTimeWarnings { get; private set; }

    /// <summary>How many times this world has sent the pilot to spectator mode.</summary>
    public int TimesSentToSpectator { get; private set; }

    /// <summary>The time limit of the terrain under the player's ship, in seconds; 0 where there is none.</summary>
    public int DockTimeLimitSecondsHere => Math.Max(0, Map?.RuleAt(Player.Position).MaxTimeAllowed ?? 0);

    /// <summary>Ticks left before the pilot is sent to spectator mode, or null where the terrain has no limit.</summary>
    public long? DockTimeLeftTicks => DockTimeLimitSecondsHere is > 0 and var limit && !PlayerIsSpectator && !Player.IsDestroyed
        ? Math.Max(0, (long)limit * TickRate - PlayerStillTicks)
        : null;

    /// <summary>
    /// Carries a still period over from a saved ship (time it stood still before it was saved plus the time since), so a relaunch
    /// or a ship pick cannot restart the clock. Call it after <see cref="RestorePlayer"/>; the limit is checked at once.
    /// </summary>
    public void ContinuePlayerStillness(long ticks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ticks);
        if (PlayerIsSpectator || Player.IsDestroyed) return;
        PlayerStillTicks = ticks;
        DockTimeWarningGiven = false;
        CheckDockTimeLimit(warn: false);
    }

    /// <summary>
    /// Brings the pilot back from spectator mode at <paramref name="position"/> (the team spawn: the emulator's unspec re-enters the
    /// arena), at rest, with the clock restarted. Hull, shield, energy, cargo and upgrades are kept, so going through spectator
    /// mode is never a free repair (Steve, Nov 3 2026, NEW for this path). Returns false when the pilot is not a spectator.
    /// </summary>
    public bool ReturnFromSpectator(Vector2 position, float heading)
    {
        if (!PlayerIsSpectator) return false;
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y) || !float.IsFinite(heading))
            throw new ArgumentException("A return position and heading must be finite.");
        PlayerIsSpectator = false;
        Player.Position = position;
        Player.Heading = MathF.IEEERemainder(heading, MathF.Tau);
        Player.Velocity = Vector2.Zero;
        RestartStillClock();
        return true;
    }

    private void RestartStillClock()
    {
        _stillPixel = PixelOf(Player.Position);
        PlayerStillTicks = 0;
        DockTimeWarningGiven = false;
    }

    // After the player's step on a tick: a whole-pixel move or an accepted shot restarts the clock, otherwise the tick counts.
    private void TrackPlayerStillness(bool fired)
    {
        var pixel = PixelOf(Player.Position);
        if (fired || pixel != _stillPixel)
        {
            _stillPixel = pixel;
            PlayerStillTicks = 0;
            DockTimeWarningGiven = false;
        }
        else
            PlayerStillTicks++;
        CheckDockTimeLimit(warn: true);
    }

    private void ElapseStillness(int ticks)
    {
        if (PlayerIsSpectator || Player.IsDestroyed) return;
        PlayerStillTicks += ticks;
        CheckDockTimeLimit(warn: true);
    }

    // The emulator's order: the warning at half the limit (once per still period), then spectator mode past the limit.
    private void CheckDockTimeLimit(bool warn)
    {
        var limit = DockTimeLimitSecondsHere;
        if (limit <= 0) return;
        var limitTicks = (long)limit * TickRate;
        if (warn && !DockTimeWarningGiven && PlayerStillTicks >= limitTicks / 2 && PlayerStillTicks <= limitTicks)
        {
            DockTimeWarningGiven = true;
            DockTimeWarnings++;
        }
        if (PlayerStillTicks > limitTicks)
        {
            PlayerIsSpectator = true;
            Player.Velocity = Vector2.Zero;
            DockTimeWarningGiven = true;
            TimesSentToSpectator++;
        }
    }

    private static (int X, int Y) PixelOf(Vector2 position) => ((int)MathF.Floor(position.X), (int)MathF.Floor(position.Y));
}
