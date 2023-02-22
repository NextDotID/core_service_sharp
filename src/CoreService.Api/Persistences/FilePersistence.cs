namespace CoreService.Api.Persistences;

using System.IO;
using System.Threading.Tasks;
using CoreService.Api.Logging;

public class FilePersistence : IPersistence
{
    private readonly string rootDirectory;
    private readonly ILogger logger;

    public FilePersistence(string rootDirectory, ILogger<FilePersistence> logger)
    {
        this.rootDirectory = rootDirectory;
        this.logger = logger;
    }

    public ValueTask DeleteAsync(string service, string? filename = null)
    {
        var servicePath = Path.Combine(rootDirectory, service);
        if (string.IsNullOrEmpty(service) || !Path.Exists(servicePath))
        {
            logger.PersistenceFileNotFound(servicePath);
            throw new DirectoryNotFoundException("Directory is not found.");
        }

        if (!string.IsNullOrEmpty(filename))
        {
            File.Delete(Path.Combine(servicePath, filename));
        }

        if (string.IsNullOrEmpty(filename) ||
            (Path.Exists(servicePath) && !Directory.GetFiles(servicePath, "*", SearchOption.AllDirectories).Any()))
        {
            Directory.Delete(servicePath, true);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<IEnumerable<string>> ListAsync(string? service = null)
    {
        if (string.IsNullOrEmpty(service))
        {
            return ValueTask.FromResult(new DirectoryInfo(rootDirectory)
                .GetDirectories()
                .Select(d => d.Name));
        }

        if (!Path.Exists(Path.Combine(rootDirectory, service)))
        {
            logger.PersistenceFileNotFound(service);
            throw new FileNotFoundException("File is not found.", service);
        }

        var dir = new DirectoryInfo(Path.Combine(rootDirectory, service));
        return ValueTask.FromResult(dir
            .GetFiles("*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(dir.FullName, f.FullName)));
    }

    public ValueTask<string> GetPathAsync(string service, string? filename = null)
    {
        if (string.IsNullOrEmpty(service) || !Path.Exists(Path.Combine(rootDirectory, service)))
        {
            logger.PersistenceFileNotFound(service);
            throw new FileNotFoundException("File is not found.", service);
        }

        var path = new string[] { rootDirectory, service, filename! }
            .Where(x => !string.IsNullOrEmpty(x))
            .ToArray();
        return ValueTask.FromResult(Path.Combine(path));
    }

    public ValueTask<Stream> ReadAsync(string service, string filename)
    {
        var path = Path.Combine(rootDirectory, service, filename);
        if (!Path.Exists(path))
        {
            logger.PersistenceFileNotFound(path);
            throw new FileNotFoundException("File is not found.", path);
        }

        Stream stream = File.OpenRead(path);
        return ValueTask.FromResult(stream);
    }

    public async ValueTask WriteAsync(string service, string filename, Stream data, bool leaveOpen = false)
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
    }
}
