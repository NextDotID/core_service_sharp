namespace CoreService.Shared.Internals;

using System.Text.Json.Serialization;

/// <summary>
/// Internal options that will be saved in persistent storage.
/// </summary>
public record Internal([property: JsonPropertyName("SUBKEY")] Subkey Subkey);
