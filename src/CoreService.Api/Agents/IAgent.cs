namespace CoreService.Api.Agents;
public interface IAgent
{
    ValueTask UpAsync(string service, string compose);

    ValueTask StopAsync(string service, string compose);

    ValueTask DownAsync(string service, string compose);

    ValueTask<IDictionary<string, bool>> ListAsync();
}
