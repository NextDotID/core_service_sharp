namespace CoreService.Api.Logging;

public static partial class LoggerMessages
{
    [LoggerMessage(1001, LogLevel.Error, "Filename is not found: {filename}")]
    public static partial void PersistenceFileNotFound(this ILogger logger, string filename);

    [LoggerMessage(2001, LogLevel.Warning, "Failed to extract point: {match}")]
    public static partial void ExtractMatchFailed(this ILogger logger, string match);
}
