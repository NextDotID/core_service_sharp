namespace CoreService.Api.Proxies;

using FluentResults;

public interface IProxy
{
    ValueTask<Result<bool>> IsRunningAsync();

    ValueTask<Result> StartAsync();

    ValueTask<Result> StopAsync();

    /// <summary>
    /// Get all routes proxied.
    /// </summary>
    /// <returns>A dictionary of which key is external route and value is internal route.</returns>
    ValueTask<Result<IDictionary<string, string>>> GetRoutesAsync();

    ValueTask<Result> SetRouteAsync(string exRoute, string inRoute);
}
