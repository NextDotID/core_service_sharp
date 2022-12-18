namespace CoreService.Api.Persistences;

using CoreService.Shared.Internals;
using FluentResults;

/// <summary>
/// Provide persistence options storage.
/// Usage: store and retrieve data that Core Service needs.
/// </summary>
public interface IPersistence
{
    ValueTask<Result<string>> LoadAsync(string key);

    ValueTask<Result> SaveAsync(string key, string data);

    ValueTask<Result<Internal>> LoadInternalAsync();

    ValueTask<Result> SaveInternalAsync(Internal internals);
}
