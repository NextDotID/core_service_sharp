namespace CoreService.Api.Vaults;
using System.Text.Json;
using System.Threading.Tasks;
using CoreService.Shared.Internals;
using LiteDB;

public class DatabaseVault : IVault
{
    private readonly ILiteCollection<BsonDocument> vault;

    public DatabaseVault(ILiteDatabase liteDatabase)
    {
        vault = liteDatabase.GetCollection("Vault");
    }

    public ValueTask<string> LoadAsync(string service, string key)
    {
        var doc = vault.FindById($"{service}__{key}");
        return doc is not null && doc.TryGetValue("value", out var v)
            ? ValueTask.FromResult(v.AsString)
            : throw new KeyNotFoundException();
    }

    public ValueTask<Internal> LoadInternalAsync()
    {
        var doc = vault.FindById("___INTERNAL");
        if (doc is null || !doc.TryGetValue("value", out var v))
        {
            throw new KeyNotFoundException();
        }

        var internals = System.Text.Json.JsonSerializer.Deserialize<Internal>(v.AsString)
            ?? throw new JsonException("Internal configurations can not be loaded.");
        return ValueTask.FromResult(internals);
    }

    public ValueTask SaveAsync(string service, string key, string data)
    {
        var doc = new BsonDocument
        {
            ["_id"] = $"{service}__{key}",
            ["value"] = data,
        };
        vault.Upsert(doc);
        return ValueTask.CompletedTask;
    }

    public ValueTask SaveInternalAsync(Internal internals)
    {
        var doc = new BsonDocument
        {
            ["_id"] = "___INTERNAL",
            ["value"] = System.Text.Json.JsonSerializer.Serialize(internals),
        };
        vault.Upsert(doc);
        return ValueTask.CompletedTask;
    }
}
