namespace CoreService.Shared.Payloads;

/// <summary>
/// API payload to prepare a service creation.
/// </summary>
/// <param name="Source">The `docker-compose.yml` file URL.</param>
public record PreparePayload(string Source);
