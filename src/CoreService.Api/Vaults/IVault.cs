namespace CoreService.Api.Vaults;

using CoreService.Shared.Internals;

/// <summary>
/// Provide vault storage to store secrets.
/// </summary>
public interface IVault
{
    ValueTask<Internal> LoadInternalAsync();

    ValueTask SaveInternalAsync(Internal internals);

    ValueTask<string> LoadAsync(string service, string key);

    ValueTask SaveAsync(string service, string key, string data);
}
