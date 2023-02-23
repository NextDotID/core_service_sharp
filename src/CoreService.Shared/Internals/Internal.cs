namespace CoreService.Shared.Internals;

using System.Text.Json.Serialization;

/// <summary>
/// Internal options that will be saved in vault.
/// </summary>
public record Internal(
    [property: JsonPropertyName("SUBKEY")]
    Subkey Subkey,
    [property: JsonPropertyName("HOST")]
    Host Host)
{
    public Internal()
        : this(new(), new())
    {
    }
}
