namespace CoreService.Api.Proxies;

using System.Collections.Generic;
using System.Threading.Tasks;
using CoreService.Api.Agents;
using CoreService.Api.Persistences;
using CoreService.Shared.Configs;
using CoreService.Shared.Proxies;
using FluentResults;

public class CaddyComposeProxy : IProxy
{
    private readonly IAgent agent;
    private readonly IPersistence persistence;
    private readonly ILogger logger;

    public CaddyComposeProxy(IAgent agent, IPersistence persistence, ILogger<CaddyComposeProxy> logger)
    {
        if (agent.Type != AgentType.Compose)
        {
            throw new ArgumentException($"Agent type must be Compose.");
        }

        this.agent = agent;
        this.persistence = persistence;
        this.logger = logger;
    }

    public ValueTask<Result<IList<ProxyRoute>>> GetRoutesAsync() => throw new NotImplementedException();

    public ValueTask<Result<bool>> IsRunningAsync() => throw new NotImplementedException();

    public ValueTask<Result> SetRouteAsync(string service, string exRoute, string inRoute) => throw new NotImplementedException();

    public ValueTask<Result> StartAsync() => throw new NotImplementedException();

    public ValueTask<Result> StopAsync() => throw new NotImplementedException();
}
