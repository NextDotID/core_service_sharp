namespace CoreService.Api.Logging;

public static partial class LoggerMessages
{
    [LoggerMessage(1001, LogLevel.Error, "Filename is not found: {filename}.")]
    public static partial void PersistenceFileNotFound(this ILogger logger, string filename);

    [LoggerMessage(2001, LogLevel.Warning, "Failed to extract point: {match}.")]
    public static partial void ExtractMatchFailed(this ILogger logger, string match);

    [LoggerMessage(3001, LogLevel.Error, "Failed to parse docker command output.")]
    public static partial void DockerOutputParseFailed(this ILogger logger, Exception ex);
}
