namespace CoreService.Shared.Payloads;

using CoreService.Shared.Injectors;

public record PrepareResponse(IEnumerable<InjectionPoint> Injections);
