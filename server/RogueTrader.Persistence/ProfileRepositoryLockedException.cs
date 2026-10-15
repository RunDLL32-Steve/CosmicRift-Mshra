namespace RogueTrader.Persistence;

public sealed class ProfileRepositoryLockedException : Exception
{
    public ProfileRepositoryLockedException(string rootDirectory, Exception? inner = null)
        : base($"Profile repository root '{rootDirectory}' is already open by another owner.", inner)
    {
        RootDirectory = rootDirectory;
    }

    public string RootDirectory { get; }
}
