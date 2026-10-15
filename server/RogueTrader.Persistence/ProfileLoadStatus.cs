namespace RogueTrader.Persistence;

public enum ProfileLoadStatus { Loaded, RecoveredFromBackup, NotFound, Corrupt, UnsupportedNewerVersion, MigrationMissing, IoError }
