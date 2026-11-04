namespace RogueTrader.Simulation;

/// <param name="Thrust">Signed thrust in [-1, 1]; negative is reverse.</param>
/// <param name="Turn">Signed rotation in [-1, 1]; positive is clockwise in screen coordinates.</param>
/// <param name="Hyper">Shift held: thrust uses the hull's boost (<see cref="RogueTrader.GameData.ShipHyper"/>) and pays energy for it.</param>
/// <param name="Strafe">Signed lateral thrust in [-1, 1]; negative is left, positive is right.</param>
public readonly record struct ShipInput(float Thrust, float Turn, bool Fire, bool Mine = false, bool Hyper = false, float Strafe = 0);

