namespace CoreService.Api.Buckets;

public interface IBucket
{
    ValueTask CloneAsync(string url);

    ValueTask<Stream> ReadFileAsync(string filename);
}
