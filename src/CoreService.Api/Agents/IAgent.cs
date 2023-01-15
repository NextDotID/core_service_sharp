namespace CoreService.Api.Agents;

using FluentResults;

public interface IAgent
{
    ValueTask<Result> StartAsync(string service);

    ValueTask<Result> StopAsync(string service);

    ValueTask<Result> RemoveAsync(string service);

    ValueTask<Result<bool>> IsRunningAsync(string service);
}
