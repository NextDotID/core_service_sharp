namespace CoreService.Shared.Configs;

public record Config(AgentConfig? Container, PersistenceConfig? Persistence, ApiConfig? Api);
