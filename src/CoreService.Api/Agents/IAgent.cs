namespace CoreService.Api.Agents;

using CoreService.Shared.Configs;
using FluentResults;

public interface IAgent
{
    AgentType Type { get; }

    ValueTask<Result<bool>> IsCreatedAsync(string service);

    ValueTask<Result<bool>> IsRunningAsync(string service);

    ValueTask<Result<bool>> CreateAsync(string service);

    ValueTask<Result> ExecuteAsync(string service, string container, string command);

    ValueTask<Result> StartAsync(string service);

    ValueTask<Result> StopAsync(string service);

    ValueTask<Result> DestroyAsync(string service);
}
