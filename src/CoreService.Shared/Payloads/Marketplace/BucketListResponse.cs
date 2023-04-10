namespace CoreService.Shared.Payloads.Marketplace;

using CoreService.Shared.Models;

public record BucketListResponse(string Name, List<Manifest> Manifests);
