using RogueTrader.Simulation;

namespace RogueTrader.AI;

/// <summary>An automatic pilot. It emits the same <see cref="ShipInput"/> a human player produces; it cannot move a ship any other way.</summary>
public interface IShipController
{
    /// <summary>Called once per fixed 60 Hz tick with the read-only view for that tick.</summary>
    ShipInput Think(AiWorldView view);
}
