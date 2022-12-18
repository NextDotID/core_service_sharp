namespace CoreService.Shared.Configs;

public enum PersistenceType
{
    // Which is dangerous.
    File,
}

public record PersistenceConfig
{
    public PersistenceType Type { get; init; } = PersistenceType.File;

    public FileConfig? File { get; init; }

    public record FileConfig(string RootDirectory);
}
