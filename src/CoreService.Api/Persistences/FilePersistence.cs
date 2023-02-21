namespace CoreService.Api.Persistences;

using System.IO;
using System.Threading.Tasks;
using CoreService.Api.Logging;
using FluentResults;

public class FilePersistence : IPersistence
{
    private readonly string rootDirectory;
    private readonly ILogger logger;

    public FilePersistence(string rootDirectory, ILogger<FilePersistence> logger)
    {
        this.rootDirectory = rootDirectory;
        this.logger = logger;
    }

    public ValueTask<Result> DeleteAsync(string service, string? filename = null)
    {
        if (string.IsNullOrEmpty(service) || !Path.Exists(Path.Combine(rootDirectory, service)))
        {
            logger.PersistenceFileNotFound(service);
            return ValueTask.FromResult(Result.Fail("service name is required"));
        }

        var path = new string[] { rootDirectory, service, filename! }
            .Where(x => !string.IsNullOrEmpty(x))
            .ToArray();

        // Delete all files in the service directory.
        Directory.Delete(Path.Combine(path));

        if (Path.Exists(Path.Combine(rootDirectory, service)) && !Directory.GetFiles(Path.Combine(rootDirectory, service)).Any())
        {
            Directory.Delete(Path.Combine(rootDirectory, service));
        }

        return ValueTask.FromResult(Result.Ok());
    }

    public ValueTask<Result<IEnumerable<string>>> ListAsync(string? service = null)
    {
        if (string.IsNullOrEmpty(service))
        {
            return ValueTask.FromResult(Result.Ok(
                new DirectoryInfo(rootDirectory).GetDirectories()
                .Select(d => d.Name)));
        }

        if (!Path.Exists(Path.Combine(rootDirectory, service)))
        {
            logger.PersistenceFileNotFound(service);
            return ValueTask.FromResult(Result.Fail<IEnumerable<string>>("service name is required"));
        }

        var dir = new DirectoryInfo(Path.Combine(rootDirectory, service));
        return ValueTask.FromResult(Result.Ok<IEnumerable<string>>(
            dir.GetFiles("*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(dir.FullName, f.FullName))
            .ToList()));
    }

    public ValueTask<Result<string>> GetPathAsync(string service, string? filename = null)
    {
        if (string.IsNullOrEmpty(service) || !Path.Exists(Path.Combine(rootDirectory, service)))
        {
            logger.PersistenceFileNotFound(service);
            return ValueTask.FromResult(Result.Fail<string>("service name is required"));
        }

        var path = new string[] { rootDirectory, service, filename! }
            .Where(x => !string.IsNullOrEmpty(x))
            .ToArray();
        return ValueTask.FromResult(Result.Ok(Path.Combine(path)));
    }

    public ValueTask<Result<Stream>> ReadAsync(string service, string filename)
    {
        var path = Path.Combine(rootDirectory, service, filename);
        if (!Path.Exists(path))
        {
            logger.PersistenceFileNotFound(path);
            return ValueTask.FromResult(Result.Fail<Stream>("file is not found"));
        }

        Stream stream = File.OpenRead(path);
        return ValueTask.FromResult(Result.Ok(stream));
    }

    public async ValueTask<Result> WriteAsync(string service, string filename, Stream data, bool leaveOpen = false)
    {
        var filePath = Path.Combine(rootDirectory, service, filename);
        var dirPath = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(dirPath) && !Path.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }

        try
        {
            await using var fileStream = File.Open(filePath, FileMode.OpenOrCreate);
            await data.CopyToAsync(fileStream);
        }
        finally
        {
            if (!leaveOpen)
            {
                data.Dispose();
            }
        }

        return Result.Ok();
    }
}
