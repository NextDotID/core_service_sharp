namespace CoreService.Shared.Internals;

using System.Text.Json.Serialization;

/// <summary>
/// Internal subkey options.
/// </summary>
/// <param name="Private">Private key in base64 format.</param>
/// <param name="Public">Public key in base64 format.</param>
public record Subkey(
    [property: JsonPropertyName("PRIVATE")]
    string Private,
    [property: JsonPropertyName("PUBLIC")]
    string Public,
    [property: JsonPropertyName("SIGNATURE")]
    string Signature);
