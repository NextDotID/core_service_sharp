namespace CoreService.Api.Logging;

public static partial class LoggerMessages
{
    [LoggerMessage(9001, LogLevel.Warning, "Failed to load the internal configuration.")]
    public static partial void InternalLoadingFailed(this ILogger logger, Exception ex);

    [LoggerMessage(9002, LogLevel.Error, "Failed to interact with the internal configuration.")]
    public static partial void InternalSetupFailed(this ILogger logger, Exception ex);

    [LoggerMessage(1001, LogLevel.Error, "Filename is not found: {filename}.")]
    public static partial void PersistenceFileNotFound(this ILogger logger, string filename);

    [LoggerMessage(2001, LogLevel.Warning, "Failed to extract point: {match}.")]
    public static partial void ExtractMatchFailed(this ILogger logger, string match);

    [LoggerMessage(3001, LogLevel.Error, "Failed to parse docker command output.")]
    public static partial void DockerOutputParseFailed(this ILogger logger, Exception ex);

    [LoggerMessage(3002, LogLevel.Error, "Failed to call docker-compose: {service} {details}.")]
    public static partial void DockerInteractionFailed(this ILogger logger, string service, string? details = null, Exception? ex = null);
}
