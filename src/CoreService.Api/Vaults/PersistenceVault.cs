namespace CoreService.Api.Vaults;

using System.Threading.Tasks;
using CoreService.Api.Persistences;
using CoreService.Shared.Internals;

public class PersistenceVault : IVault
{
    private const string VaultService = "Vault";
    private const string InternalFilename = "internal.json";

    private readonly IPersistence persistence;

    public PersistenceVault(IPersistence persistence)
    {
        this.persistence = persistence;
    }

    public ValueTask<string> LoadAsync(string service, string key)
    {
        return persistence.ReadTextAsync(VaultService, string.Concat(service, "/", key));
    }

    public ValueTask<Internal> LoadInternalAsync()
    {
        return persistence.ReadJsonAsync<Internal>(VaultService, InternalFilename)!;
    }

    public ValueTask SaveAsync(string service, string key, string data)
    {
        return persistence.WriteTextAsync(VaultService, string.Concat(service, "/", key), data);
    }

    public ValueTask SaveInternalAsync(Internal internals)
    {
        return persistence.WriteJsonAsync(VaultService, InternalFilename, internals);
    }
}
