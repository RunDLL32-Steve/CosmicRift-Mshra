namespace RogueTrader.Simulation;

/// <param name="Thrust">Signed thrust in [-1, 1]; negative is reverse.</param>
/// <param name="Turn">Signed rotation in [-1, 1]; positive is clockwise in screen coordinates.</param>
public readonly record struct ShipInput(float Thrust, float Turn, bool Fire, bool Mine = false);

