namespace CoreService.Api.Agents;
public interface IAgent
{
    ValueTask UpAsync(string service);

    ValueTask StopAsync(string service);

    ValueTask DownAsync(string service);

    ValueTask<IDictionary<string, bool>> ListAsync();
}
