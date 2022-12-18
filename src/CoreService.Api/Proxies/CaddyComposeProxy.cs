namespace CoreService.Api.Proxies;

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CoreService.Api.Agents;
using CoreService.Api.Logging;
using CoreService.Api.Persistences;
using CoreService.Shared.Configs;
using CoreService.Shared.Proxies;
using FluentResults;

public class CaddyComposeProxy : IProxy
{
    private const string CaddyfilePersistence = "core/caddyfile";
    private const string CaddyServiceName = "core_caddy";
    private const string CaddyContainerName = "caddy";
    private const string CaddyApiEndpoint = "http://caddy:2019/";

    private readonly IAgent agent;
    private readonly IPersistence persistence;
    private readonly ILogger logger;

    public CaddyComposeProxy(IAgent agent, IPersistence persistence, ILogger<CaddyComposeProxy> logger)
    {
        if (agent.Type != AgentType.Compose)
        {
            throw new ArgumentException($"Agent type must be {nameof(AgentType.Compose)}");
        }

        this.agent = agent;
        this.persistence = persistence;
        this.logger = logger;
    }

    public async ValueTask<Result<IList<ProxyRoute>>> GetRoutesAsync()
    {
        var caddyfileRes = await ReadCaddyfileAsync();
        if (caddyfileRes.IsFailed)
        {
            return caddyfileRes.ToResult();
        }

        return caddyfileRes.Value.Apps.Http.Servers
            .SelectMany(s => s.Value.Routes.Select(r =>
            {
                var service = s.Key!;
                var handle = (ReverseProxyHandle?)r.Handle.FirstOrDefault(h => h is ReverseProxyHandle);
                var inRoute = handle?.Upstreams.FirstOrDefault()?.Dial;
                var exRoute = r.Match.FirstOrDefault()?.Path.FirstOrDefault();

                if (inRoute is null || exRoute is null)
                {
                    logger.GetRoutesPartInvalid(service, r.Match, handle);
                    return null;
                }

                return new ProxyRoute(service, exRoute, inRoute);
            }))
            .Where(r => r is not null)
            .Select(r => r!)
            .ToList();
    }

    public ValueTask<Result<bool>> IsRunningAsync() => agent.IsRunningAsync(CaddyServiceName);

    public async ValueTask<Result> SetRouteAsync(ProxyRoute route)
    {
        var caddyfileRes = await ReadCaddyfileAsync();
        if (caddyfileRes.IsFailed)
        {
            return caddyfileRes.ToResult();
        }

        var caddyfile = caddyfileRes.Value;

        // TODO: Validate routes before set.
        var server = BuildServerConfig(route);
        caddyfile.Apps.Http.Servers[route.Service] = server;

        var json = JsonSerializer.Serialize(caddyfile);
        var saveRes = await persistence.SaveAsync(CaddyfilePersistence, json);
        if (saveRes.IsFailed)
        {
            return saveRes;
        }

        return await agent.ExecuteAsync(
            CaddyServiceName,
            CaddyContainerName,
            $"curl -X POST -H \"Content-Type: application/json\" -d '{json}' \"http://localhost:2019/load\"");
    }

    public ValueTask<Result> StartAsync() => throw new NotImplementedException();

    public ValueTask<Result> StopAsync() => throw new NotImplementedException();

    private static Server BuildServerConfig(ProxyRoute route)
    {
        var match = new List<Match>();
        var handle = new List<Handle>();

        // By default, all services listen to port 80 and 443 only.
        var server = new Server(new() { new Route(match, handle) }, new() { ":80", ":443" });

        // Matches all path. See https://caddyserver.com/docs/json/apps/http/servers/routes/match/path/
        match.Add(new Match(new() { string.Concat(route.ExternalRoute, "*") }));

        // Strip URL prefix (external route) first.
        handle.Add(new RewriteHandle(route.ExternalRoute));

        handle.Add(new ReverseProxyHandle(new() { new Upstream(route.InternalRoute) }));
        return server;
    }

    private async ValueTask<Result<Caddyfile>> ReadCaddyfileAsync()
    {
        // TODO: Read from Caddy API. See https://caddyserver.com/docs/quick-starts/api
        var fileRes = await persistence.LoadAsync(CaddyfilePersistence);
        if (fileRes.IsFailed)
        {
            logger.GetRoutesNotFound();
            return fileRes.ToResult();
        }

        var caddyfile = JsonSerializer.Deserialize<Caddyfile>(fileRes.Value);
        if (caddyfile is null)
        {
            logger.GetRoutesInvalid();
            return Result.Fail("caddyfile is invalid");
        }

        return caddyfile;
    }

    private sealed record Caddyfile(
        [property: JsonPropertyName("admin")]
        Admin Admin,
        [property: JsonPropertyName("apps")]
        Apps Apps);

    private sealed record Admin([property: JsonPropertyName("listen")] string Listen);

    private sealed record Apps([property: JsonPropertyName("http")] Http Http);

    private sealed record Http([property: JsonPropertyName("servers")] SortedDictionary<string, Server> Servers);

    private sealed record Server(
        [property: JsonPropertyName("routes")]
        List<Route> Routes,
        [property: JsonPropertyName("listen")]
        List<string> Listen);

    private sealed record Route(
        [property: JsonPropertyName("match")]
        List<Match> Match,
        [property: JsonPropertyName("handle")]
        List<Handle> Handle);

    private sealed record Match([property: JsonPropertyName("path")] List<string> Path);

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "handler")]
    [JsonDerivedType(typeof(ReverseProxyHandle), "reverse_proxy")]
    [JsonDerivedType(typeof(RewriteHandle), "rewrite")]
    private abstract record Handle();

    private sealed record RewriteHandle([property: JsonPropertyName("strip_path_prefix")] string StripPathPrefix) : Handle();

    private sealed record ReverseProxyHandle([property: JsonPropertyName("upstreams")] List<Upstream> Upstreams) : Handle();

    private sealed record Upstream(string Dial);
}
