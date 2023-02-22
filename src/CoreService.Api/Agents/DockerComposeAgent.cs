namespace CoreService.Api.Agents;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Buffered;
using CoreService.Api.Logging;
using CoreService.Api.Persistences;

public class DockerComposeAgent : IAgent
{
    private readonly IPersistence persistence;
    private readonly ILogger logger;

    public DockerComposeAgent(
        IPersistence persistence,
        ILogger<DockerComposeAgent> logger)
    {
        this.persistence = persistence;
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
            var list = JsonSerializer.Deserialize<DockerComposeLsOutput[]>(cmd.StandardOutput);
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

    public async ValueTask DownAsync(string service)
    {
        var cmd = await BuildCommandAsync(service);
        await cmd.WithArguments("down --remove-orphans").ExecuteAsync();
    }

    public async ValueTask UpAsync(string service)
    {
        var cmd = await BuildCommandAsync(service);
        await cmd.WithArguments("up -d").ExecuteAsync();
    }

    public async ValueTask StopAsync(string service)
    {
        var cmd = await BuildCommandAsync(service);
        await cmd.WithArguments("stop").ExecuteAsync();
    }

    private async ValueTask<Command> BuildCommandAsync(string service)
    {
        var path = await persistence.GetPathAsync(service);
        return Cli.Wrap("docker-compose").WithWorkingDirectory(path);
    }

    private sealed record DockerComposeLsOutput(string Name, string Status, string ConfigFiles);
}
