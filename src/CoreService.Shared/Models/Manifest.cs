namespace CoreService.Shared.Models;

using System.Text.Json.Serialization;

public record Manifest(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("icon")] string Icon,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("homepage")] string Homepage,
    [property: JsonPropertyName("license")] string License,
    [property: JsonPropertyName("compatibility")] Compatibility Compatibility,
    [property: JsonPropertyName("permissions")] IReadOnlyList<string> Permissions,
    [property: JsonPropertyName("architecture")] IReadOnlyDictionary<string, ArchitectureDetail> Architecture);

public record Compatibility(
    [property: JsonPropertyName("minimum")] string Minimum,
    [property: JsonPropertyName("compatible")] string Compatible);

public record ArchitectureDetail([property: JsonPropertyName("url")] string Url);
