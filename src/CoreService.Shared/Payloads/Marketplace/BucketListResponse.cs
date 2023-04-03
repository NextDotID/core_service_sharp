namespace CoreService.Shared.Payloads.Marketplace;

public record BucketListResponse(string Name, List<Manifest> Manifests);
