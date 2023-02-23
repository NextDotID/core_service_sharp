namespace CoreService.Api.Agents;
using System.Text;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Buffered;
using CoreService.Api.Logging;

public class DockerComposeAgent : IAgent
{
    private readonly ILogger logger;

    public DockerComposeAgent(ILogger<DockerComposeAgent> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc/>
    public async ValueTask<IDictionary<string, bool>> ListAsync()
    {
        var result = new Dictionary<string, bool>();
        var stdout = new StringBuilder();
        var cmd = await Cli.Wrap("docker-compose")
            .WithArguments("ls --all --format json")
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdout))
            .ExecuteBufferedAsync();

        try
        {
            var list = System.Text.Json.JsonSerializer.Deserialize<DockerComposeLsOutput[]>(cmd.StandardOutput);
            foreach (var service in list ?? Array.Empty<DockerComposeLsOutput>())
            {
                result.TryAdd(service.Name, !service.Status.Contains("exited"));
            }
        }
        catch (Exception ex)
        {
            logger.DockerOutputParseFailed(ex);
            throw new ArgumentException("Invalid docker-compose output.", ex);
        }

        return result;
    }

    public async ValueTask DownAsync(string service, string compose)
    {
        var cmd = await BuildCommandAsync(service, compose);
        var result = await cmd.WithArguments("down --remove-orphans")
            .ExecuteBufferedAsync();

        if (result.ExitCode != 0)
        {
            logger.DockerInteractionFailed(service, result.StandardError);
            throw new InvalidOperationException("Failed to operate docker-compose.");
        }
    }

    public async ValueTask UpAsync(string service, string compose)
    {
        var cmd = await BuildCommandAsync(service, compose);
        var result = await cmd.WithArguments("up -d")
            .ExecuteBufferedAsync();

        if (result.ExitCode != 0)
        {
            logger.DockerInteractionFailed(service, result.StandardError);
            throw new InvalidOperationException("Failed to operate docker-compose.");
        }
    }

    public async ValueTask StopAsync(string service, string compose)
    {
        var cmd = await BuildCommandAsync(service, compose);
        var result = await cmd.WithArguments("stop")
            .ExecuteBufferedAsync();

        if (result.ExitCode != 0)
        {
            logger.DockerInteractionFailed(service, result.StandardError);
            throw new InvalidOperationException("Failed to operate docker-compose.");
        }
    }

    private static async ValueTask<Command> BuildCommandAsync(string service, string compose)
    {
        var stderr = new StringBuilder();
        var temp = Directory.CreateTempSubdirectory(service);
        var path = Path.Combine(temp.FullName, service);
        var dir = Directory.CreateDirectory(path);

        await File.WriteAllTextAsync(Path.Combine(dir.FullName, "docker-compose.yml"), compose);

        return Cli.Wrap("docker-compose")
            .WithWorkingDirectory(dir.FullName)
            .WithValidation(CommandResultValidation.None)
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stderr));
    }

    private sealed record DockerComposeLsOutput(string Name, string Status, string ConfigFiles);
}
