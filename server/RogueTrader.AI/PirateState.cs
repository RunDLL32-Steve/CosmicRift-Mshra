namespace RogueTrader.AI;

/// <summary>Plan 12.2 initial state machine. Dead is a terminal state added for destroyed ships.</summary>
public enum PirateState
{
    Idle,
    Patrol,
    DetectTarget,
    Chase,
    Attack,
    Evade,
    ReturnToPatrol,
    Dead
}
