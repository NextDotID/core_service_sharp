namespace CoreService.Api.Vaults;

using System.Threading.Tasks;
using CoreService.Api.Persistences;
using CoreService.Shared.Internals;
using FluentResults;

public class PersistenceVault : IVault
{
    private const string VaultService = "Vault";
    private const string InternalFilename = "internal.json";

    private readonly IPersistence persistence;

    public PersistenceVault(IPersistence persistence)
    {
        this.persistence = persistence;
    }

    public ValueTask<Result<string>> LoadAsync(string service, string key)
    {
        return persistence.ReadTextAsync(VaultService, string.Concat(service, "/", key));
    }

    public ValueTask<Result<Internal>> LoadInternalAsync()
    {
        return persistence.ReadJsonAsync<Internal>(VaultService, InternalFilename);
    }

    public ValueTask<Result> SaveAsync(string service, string key, string data)
    {
        return persistence.WriteTextAsync(VaultService, string.Concat(service, "/", key), data);
    }

    public ValueTask<Result> SaveInternalAsync(Internal internals)
    {
        return persistence.WriteJsonAsync(VaultService, InternalFilename, internals);
    }
}
