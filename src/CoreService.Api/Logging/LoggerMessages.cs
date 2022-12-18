namespace CoreService.Api.Logging;

public static partial class LoggerMessages
{
    [LoggerMessage(2001, LogLevel.Warning, "Failed to extract point: {match}")]
    public static partial void ExtractMatchFailed(this ILogger logger, string match);

    [LoggerMessage(1001, LogLevel.Error, "Key is invalid: {key}")]
    public static partial void LoadKeyInvalid(this ILogger logger, string key);

    [LoggerMessage(1002, LogLevel.Error, "File is not found: {path}")]
    public static partial void LoadKeyFileNotFound(this ILogger logger, string path);

    [LoggerMessage(1003, LogLevel.Error, "File is not found: {path}")]
    public static partial void LoadInternalFileNotFound(this ILogger logger, string path);

    [LoggerMessage(1004, LogLevel.Error, "Key is invalid: {key}")]
    public static partial void SaveKeyInvalid(this ILogger logger, string key);
}
