namespace CoreService.Shared.Payloads;

public record ServiceStatus(string Name, bool IsRunning, IEnumerable<string> Endpoints);
