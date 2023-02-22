namespace CoreService.Api.Persistences;

using System.Text;
using System.Text.Json;

/// <summary>
/// Provide persistence storage.
/// </summary>
public interface IPersistence
{
    ValueTask WriteAsync(string service, string filename, Stream data, bool leaveOpen = false);

    ValueTask<Stream> ReadAsync(string service, string filename);

    ValueTask DeleteAsync(string service, string? filename = null);

    ValueTask<IEnumerable<string>> ListAsync(string? service = null);

    ValueTask<string> GetPathAsync(string service, string? filename = null);

    ValueTask WriteBytesAsync(string service, string filename, Span<byte> data)
    {
        using var stream = new MemoryStream(data.ToArray());
        return WriteAsync(service, filename, stream);
    }

    ValueTask WriteTextAsync(string service, string filename, string data)
    {
        return WriteBytesAsync(service, filename, Encoding.UTF8.GetBytes(data));
    }

    async ValueTask WriteJsonAsync<T>(string service, string filename, T toSerialize)
    {
        using var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, toSerialize);
        stream.Seek(0, SeekOrigin.Begin);
        await WriteAsync(service, filename, stream);
    }

    async ValueTask<string> ReadTextAsync(string service, string filename)
    {
        var stream = await ReadAsync(service, filename);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    async ValueTask<T?> ReadJsonAsync<T>(string service, string filename)
    {
        var stream = await ReadAsync(service, filename);
        return await JsonSerializer.DeserializeAsync<T>(stream);
    }
}
