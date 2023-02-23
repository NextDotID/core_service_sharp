namespace CoreService.Shared.Internals;

using System.Text.Json.Serialization;

/// <summary>
/// Internal subkey options.
/// </summary>
/// <param name="Private">Private key in hex format.</param>
/// <param name="Public">Public key in hex format.</param>
/// <param name="Avatar">Public key of <b>Avatar</b>, in hex format.</param>
/// <param name="Signature">Signature to certify Subkey, in hex format.</param>
public record Subkey(
    [property: JsonPropertyName("PRIVATE")]
    string Private,
    [property: JsonPropertyName("PUBLIC")]
    string Public,
    [property: JsonPropertyName("AVATAR")]
    string Avatar,
    [property: JsonPropertyName("SIGNATURE")]
    string Signature)
{
    public Subkey()
        : this(string.Empty, string.Empty, string.Empty, string.Empty)
    {
    }
}
