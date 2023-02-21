namespace CoreService.Api.Agents;

using System.Threading.Tasks;
using CliWrap;
using CoreService.Api.Persistences;
using FluentResults;

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

    public async ValueTask<Result<bool>> IsRunningAsync(string service)
    {
        var cmd = await BuildCommandAsync(service);
        await cmd.Value.WithArguments("inspect").ExecuteAsync();
        return Result.Ok(true);
    }

    public async ValueTask<Result> DownAsync(string service)
    {
        var cmd = await BuildCommandAsync(service);
        await cmd.Value.WithArguments("down --remove-orphans").ExecuteAsync();
        return Result.Ok();
    }

    public async ValueTask<Result> UpAsync(string service)
    {
        var cmd = await BuildCommandAsync(service);
        await cmd.Value.WithArguments("up -d").ExecuteAsync();
        return Result.Ok();
    }

    public async ValueTask<Result> StopAsync(string service)
    {
        var cmd = await BuildCommandAsync(service);
        await cmd.Value.WithArguments("stop").ExecuteAsync();
        return Result.Ok();
    }

    private async ValueTask<Result<Command>> BuildCommandAsync(string service)
    {
        var path = await persistence.GetPathAsync(service);

        return path switch
        {
            { IsSuccess: true } => Cli.Wrap("docker-compose").WithWorkingDirectory(service).ToResult(),
            _ => path.ToResult(),
        };
    }
}
