namespace CoreService.Api.Agents;

using FluentResults;

public interface IAgent
{
    ValueTask<Result> UpAsync(string service);

    ValueTask<Result> StopAsync(string service);

    ValueTask<Result> DownAsync(string service);

    ValueTask<Result<bool>> IsRunningAsync(string service);
}
