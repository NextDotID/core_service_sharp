namespace CoreService.Api.Proxies;

using FluentResults;

public interface IProxy
{
    Result<string> PoplulateRoute(string input, string service);
}
