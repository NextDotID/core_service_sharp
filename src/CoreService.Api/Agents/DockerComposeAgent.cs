namespace CoreService.Api.Agents;

using System.Threading.Tasks;
using CoreService.Api.Persistences;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Services;
using FluentResults;

public class DockerComposeAgent : IAgent
{
    private const string ComposeFile = "docker-compose.yml";

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
        var svcRes = await BuildServiceAsync(service);
        if (svcRes.IsFailed)
        {
            return svcRes.ToResult();
        }

        using var svc = svcRes.Value;
        return svc.Services.All(s => s.State == ServiceRunningState.Running);
    }

    public async ValueTask<Result> RemoveAsync(string service)
    {
        var svcRes = await BuildServiceAsync(service);
        if (svcRes.IsFailed)
        {
            return svcRes.ToResult();
        }

        using var svc = svcRes.Value;
        svc.Remove(true);
        return Result.Ok();
    }

    public async ValueTask<Result> StartAsync(string service)
    {
        var svcRes = await BuildServiceAsync(service);
        if (svcRes.IsFailed)
        {
            return svcRes.ToResult();
        }

        using var svc = svcRes.Value;
        svc.Start();
        return Result.Ok();
    }

    public async ValueTask<Result> StopAsync(string service)
    {
        var svcRes = await BuildServiceAsync(service);
        if (svcRes.IsFailed)
        {
            return svcRes.ToResult();
        }

        using var svc = svcRes.Value;
        svc.Stop();
        return Result.Ok();
    }

    private async ValueTask<Result<ICompositeService>> BuildServiceAsync(string service)
    {
        var pathRes = await persistence.GetPathAsync(service, ComposeFile);
        if (pathRes.IsFailed)
        {
            return pathRes.ToResult();
        }

        var path = pathRes.Value;
        return new Builder()
            .UseContainer()
            .UseCompose()
            .FromFile(path)
            .KeepRunning()
            .Build()
            .ToResult();
    }
}
