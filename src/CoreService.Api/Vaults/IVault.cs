namespace CoreService.Api.Vaults;

using CoreService.Shared.Internals;
using FluentResults;

/// <summary>
/// Provide vault storage to store secrets.
/// </summary>
public interface IVault
{
    ValueTask<Result<Internal>> LoadInternalAsync();

    ValueTask<Result> SaveInternalAsync(Internal internals);

    ValueTask<Result<string>> LoadAsync(string service, string key);

    ValueTask<Result> SaveAsync(string service, string key, string data);
}
