namespace RogueTrader.Persistence;

/// <summary>One id in a profile that the content catalog rejected.</summary>
public sealed record ProfileContentViolation(ProfileContentField Field, string Id);
