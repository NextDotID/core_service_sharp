namespace CoreService.Shared.Configs;

public enum AgentType
{
    Compose,
    Kubernetes,
}

public record AgentConfig
{
    public AgentType Type { get; init; } = AgentType.Compose;

    public ComposeConfig? Docker { get; init; }

    public record ComposeConfig(string Socket);
}
