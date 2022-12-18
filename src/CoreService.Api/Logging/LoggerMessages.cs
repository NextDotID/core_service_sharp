namespace CoreService.Api.Logging;

public static partial class LoggerMessages
{
    [LoggerMessage(1001, LogLevel.Error, "Key is invalid: {key}")]
    public static partial void LoadKeyInvalid(this ILogger logger, string key);

    [LoggerMessage(1002, LogLevel.Error, "File is not found: {path}")]
    public static partial void LoadKeyFileNotFound(this ILogger logger, string path);

    [LoggerMessage(1003, LogLevel.Error, "File is not found: {path}")]
    public static partial void LoadInternalFileNotFound(this ILogger logger, string path);

    [LoggerMessage(1004, LogLevel.Error, "Key is invalid: {key}")]
    public static partial void SaveKeyInvalid(this ILogger logger, string key);

    [LoggerMessage(2001, LogLevel.Warning, "Failed to extract point: {match}")]
    public static partial void ExtractMatchFailed(this ILogger logger, string match);

    [LoggerMessage(3001, LogLevel.Error, "Route info is not found")]
    public static partial void GetRoutesNotFound(this ILogger logger);

    [LoggerMessage(3002, LogLevel.Error, "Route info is invalid")]
    public static partial void GetRoutesInvalid(this ILogger logger);

    [LoggerMessage(3003, LogLevel.Error, "Route info part is invalid: {service} {match} {proxy}")]
    public static partial void GetRoutesPartInvalid(this ILogger logger, string service, object? match, object? proxy);
}
