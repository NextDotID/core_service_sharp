namespace CoreService.Api.Agents;
public interface IAgent
{
    ValueTask UpAsync(string service, string compose);

    ValueTask StopAsync(string service, string compose);

    ValueTask DownAsync(string service, string compose);

    /// <summary>
    ///    List all services.
    /// </summary>
    /// <returns>Service name as the key, whether it is Running as the value.</returns>
    ValueTask<IDictionary<string, bool>> ListAsync();
}
