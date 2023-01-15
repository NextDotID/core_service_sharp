namespace CoreService.Shared.Payloads;

public record ServicesResponse(IEnumerable<ServiceStatus> Services);
