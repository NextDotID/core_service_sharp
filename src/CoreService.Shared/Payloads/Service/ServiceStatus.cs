namespace CoreService.Shared.Payloads.Service;

public record ServiceStatus(string Name, bool IsRunning, IEnumerable<string> Endpoints);
