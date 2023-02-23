namespace CoreService.Shared.Internals;

using System.Text.Json.Serialization;

public record Host([property: JsonPropertyName("DOMAIN")] string Domain)
{
    public Host()
        : this(string.Empty)
    {
    }
}
