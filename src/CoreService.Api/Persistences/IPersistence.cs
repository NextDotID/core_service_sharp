namespace CoreService.Api.Persistences;

using CoreService.Shared.Internals;
using FluentResults;

public interface IPersistence
{
    ValueTask<Result<string>> LoadAsync(string key);

    ValueTask<Result> SaveAsync(string key, string data);

    ValueTask<Result<Internal>> LoadInternalAsync();

    ValueTask<Result> SaveInternalAsync(Internal internals);
}
