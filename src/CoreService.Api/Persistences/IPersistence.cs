namespace CoreService.Api.Persistences;

using System.Text;
using System.Text.Json;
using FluentResults;

/// <summary>
/// Provide persistence storage.
/// </summary>
public interface IPersistence
{
    ValueTask<Result> WriteAsync(string service, string filename, Stream data, bool leaveOpen = false);

    ValueTask<Result<Stream>> ReadAsync(string service, string filename);

    ValueTask<Result> DeleteAsync(string service, string? filename = null);

    ValueTask<Result<IEnumerable<string>>> ListAsync(string? service = null);

    ValueTask<Result<string>> GetPathAsync(string service, string? filename = null);

    ValueTask<Result<string>> GetAbsolutePathAsync(string service, string? filename = null);

    ValueTask<Result> WriteBytesAsync(string service, string filename, Span<byte> data)
    {
        using var stream = new MemoryStream(data.ToArray());
        return WriteAsync(service, filename, stream);
    }

    ValueTask<Result> WriteTextAsync(string service, string filename, string data)
    {
        return WriteBytesAsync(service, filename, Encoding.UTF8.GetBytes(data));
    }

    async ValueTask<Result> WriteJsonAsync<T>(string service, string filename, T toSerialize)
    {
        using var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, toSerialize);
        stream.Seek(0, SeekOrigin.Begin);
        return await WriteAsync(service, filename, stream);
    }

    async ValueTask<Result<string>> ReadTextAsync(string service, string filename)
    {
        var result = await ReadAsync(service, filename);
        if (result.IsFailed)
        {
            return result.ToResult();
        }

        using var stream = result.Value;
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    async ValueTask<Result<T>> ReadJsonAsync<T>(string service, string filename)
    {
        var result = await ReadAsync(service, filename);
        if (result.IsFailed)
        {
            return result.ToResult();
        }

        using var stream = result.Value;
        var data = await JsonSerializer.DeserializeAsync<T>(stream);
        return data ?? Result.Fail<T>("invalid data");
    }
}
