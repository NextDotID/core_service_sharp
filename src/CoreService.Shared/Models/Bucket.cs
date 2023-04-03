namespace CoreService.Shared.Models;

public class Bucket
{
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets service name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the repository url of the bucket.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the last time the bucket was pulled/cloned.
    /// </summary>
    public DateTimeOffset PulledAt { get; set; } = DateTimeOffset.MinValue;
}
