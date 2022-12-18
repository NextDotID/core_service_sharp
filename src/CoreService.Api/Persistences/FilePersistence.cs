namespace CoreService.Api.Persistences;

using System.Text.Json;
using System.Threading.Tasks;
using CoreService.Api.Logging;
using CoreService.Shared.Internals;
using FluentResults;

public class FilePersistence : IPersistence
{
    private const string InternalFileName = "internal.json";
    private readonly string rootDir;

    private readonly ILogger logger;

    public FilePersistence(string rootDir, ILogger<FilePersistence> logger)
    {
        this.rootDir = rootDir;
        this.logger = logger;
    }

    public async ValueTask<Result<string>> LoadAsync(string key)
    {
        if (string.Equals(key, InternalFileName, StringComparison.OrdinalIgnoreCase))
        {
            logger.LoadKeyInvalid(key);
            return Result.Fail("invalid key");
        }

        var path = Path.Combine(rootDir, key);
        if (!File.Exists(path))
        {
            logger.LoadKeyFileNotFound(path);
            return Result.Fail("file not found");
        }

        return await File.ReadAllTextAsync(path);
    }

    public async ValueTask<Result<Internal>> LoadInternalAsync()
    {
        var path = Path.Combine(rootDir, InternalFileName);
        if (!File.Exists(path))
        {
            logger.LoadInternalFileNotFound(path);
            return Result.Fail("file not found");
        }

        await using var stream = File.OpenRead(path);
        var internals = await JsonSerializer.DeserializeAsync<Internal>(stream);
        return internals ?? Result.Fail<Internal>("invalid internal");
    }

    public async ValueTask<Result> SaveAsync(string key, string data)
    {
        if (string.Equals(key, InternalFileName, StringComparison.OrdinalIgnoreCase))
        {
            logger.SaveKeyInvalid(key);
            return Result.Fail("invalid key");
        }

        var path = Path.Combine(rootDir, key);
        await File.WriteAllTextAsync(path, data);
        return Result.Ok();
    }

    public async ValueTask<Result> SaveInternalAsync(Internal internals)
    {
        var path = Path.Combine(rootDir, InternalFileName);
        await using var stream = File.OpenWrite(path);
        await JsonSerializer.SerializeAsync(stream, internals);
        return Result.Ok();
    }
}
