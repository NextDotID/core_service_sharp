namespace CoreService.Shared.Payloads.Service;

using CoreService.Shared.Injectors;

public record PrepareResponse(IEnumerable<InjectionPoint> Injection, IDictionary<string, string> Prompted);
