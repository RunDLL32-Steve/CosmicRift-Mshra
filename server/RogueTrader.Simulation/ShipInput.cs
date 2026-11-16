namespace RogueTrader.Simulation;

/// <param name="Thrust">Signed thrust in [-1, 1]; negative is reverse.</param>
/// <param name="Turn">Signed rotation in [-1, 1]; positive is clockwise in screen coordinates.</param>
/// <param name="Mine">Mining: extract from an asteroid in range (queue M3), not a space mine.</param>
/// <param name="Hyper">Shift held: thrust uses the hull's boost (<see cref="RogueTrader.GameData.ShipHyper"/>) and pays energy for it.</param>
/// <param name="LayMine">Drop a space mine from the hold (RT-F038), if the ship carries one and may lay another now.</param>
public readonly record struct ShipInput(float Thrust, float Turn, bool Fire, bool Mine = false, bool Hyper = false, bool LayMine = false);
