namespace CoreService.Shared.Models;
public class Service
{
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets service name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this service is created.
    /// </summary>
    public bool IsCreated { get; set; }

    /// <summary>
    /// Gets or sets the raw file content of `docker-compose.yml`.
    /// </summary>
    public string Compose { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the available endpoints of this service.
    /// </summary>
    public int[] Ports { get; set; } = Array.Empty<int>();

    /// <summary>
    /// Gets or sets the user prompts.
    /// </summary>
    public Dictionary<string, string> Prompted { get; set; } = new();

    public Dictionary<string, string> Generated { get; set; } = new();
}
