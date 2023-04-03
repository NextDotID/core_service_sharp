namespace CoreService.Api.Controllers;

using System.Text.Json;
using CoreService.Api.Logging;
using CoreService.Shared.Models;
using CoreService.Shared.Payloads.Marketplace;
using LibGit2Sharp;
using LiteDB;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class MarketplaceController : ControllerBase
{
    private const string BucketInfoJsonPath = "bucket.json";
    private const string BucketManifestsPath = "manifest/";

    private readonly ILiteDatabase liteDatabase;
    private readonly ILogger logger;

    private readonly string bucketDirectory;

    public MarketplaceController(
        ILiteDatabase liteDatabase,
        IConfiguration configuration,
        ILogger<MarketplaceController> logger)
    {
        this.liteDatabase = liteDatabase;
        this.logger = logger;

        bucketDirectory = configuration["Marketplace:BucketDirectory"]
            ?? throw new ArgumentNullException(nameof(configuration), $"{nameof(bucketDirectory)} is not configured.");
    }

    /// <summary>
    ///     Clone a bucket.
    /// </summary>
    /// <param name="payload">The parameters to clone the bucket.</param>
    /// <returns>Ok.</returns>
    /// <response code="200">If cloned correctly.</response>
    /// <response code="400">If the repo is not a valid core_service bucket repo.</response>
    /// <response code="409">If already cloned.</response>
    /// <response code="500">If clone failed.</response>
    [HttpPost("clone")]
    public async ValueTask<ActionResult> CloneAsync(ClonePayload payload)
    {
        if (!Uri.TryCreate(payload.Url, UriKind.Absolute, out var uri))
        {
            return Problem("Url is invalid.", null, StatusCodes.Status400BadRequest);
        }

        var url = uri.ToString();
        string path;
        try
        {
            path = Repository.Clone(url, bucketDirectory);
        }
        catch (RecurseSubmodulesException rse)
        {
            logger.MarketplaceCloneFailed(url, rse);
            return Problem("Repository includes submodule.", null, StatusCodes.Status400BadRequest);
        }
        catch (Exception ex)
        {
            logger.MarketplaceCloneFailed(url, ex);
            return Problem("Repository is invalid.", null, StatusCodes.Status500InternalServerError);
        }

        await using var bucketInfoStream = System.IO.File.OpenRead(Path.Combine(path, BucketInfoJsonPath));
        var info = await System.Text.Json.JsonSerializer.DeserializeAsync<BucketInfo>(bucketInfoStream)
            ?? throw new JsonException();

        var bucketCol = liteDatabase.GetCollection<Bucket>();
        if (bucketCol.Exists(b => b.Name == info.Name))
        {
            return Problem("Bucket with the same name already exists.", null, StatusCodes.Status409Conflict);
        }

        var bucket = new Bucket { Name = info.Name, PulledAt = DateTimeOffset.Now, Url = url };
        _ = bucketCol.Insert(bucket);

        return Ok();
    }

    /// <summary>
    ///     List applications from a bucket.
    /// </summary>
    /// <param name="bucketName">Bucket name.</param>
    /// <returns>Ok.</returns>
    /// <response code="200">Application list.</response>
    /// <response code="404">If a bucket with the name is not found or does not have manifests.</response>
    [HttpGet("list/{bucket}")]
    public async ValueTask<ActionResult<BucketListResponse>> ListAsync(string bucketName)
    {
        var bucketCol = liteDatabase.GetCollection<Bucket>();
        var bucket = bucketCol.FindOne(b => b.Name == bucketName);
        var manifestDir = Path.Combine(bucketDirectory, bucket.Name, BucketManifestsPath);
        if (bucket is null
            || !Directory.Exists(manifestDir)
            || !Directory.GetFiles(manifestDir, "*.json").Any())
        {
            return Problem("Bucket with the name does not exist.", null, StatusCodes.Status404NotFound);
        }

        var files = Directory.GetFiles(manifestDir, "*.json");
        var manifests = new List<Manifest>(files.Length);
        foreach (var file in files)
        {
            await using var stream = System.IO.File.OpenRead(file);
            var manifest = await System.Text.Json.JsonSerializer.DeserializeAsync<Manifest>(stream);
            if (manifest is not null)
            {
                manifests.Add(manifest);
            }
        }

        // TODO: cache list.
        return Ok(new BucketListResponse(bucket.Name, manifests));
    }

    /// <summary>
    ///     List applications from all buckets.
    /// </summary>
    /// <returns>Ok.</returns>
    /// <response code="200">Application list from all buckets.</response>
    /// <response code="404">If there is no valid buckets.</response>
    [HttpPost("list")]
    public async ValueTask<ActionResult<BucketListResponse>> ListAllAsync()
    {
        var bucketCol = liteDatabase.GetCollection<Bucket>();
        var buckets = bucketCol.Find(b => b.PulledAt > DateTimeOffset.MinValue);

        if (!buckets.Any())
        {
            return Problem("No valid buckets.", null, StatusCodes.Status404NotFound);
        }

        var bucketLists = new List<BucketListResponse>();
        foreach (var bucketName in buckets.Select(b => b.Name))
        {
            var manifestDir = Path.Combine(bucketDirectory, bucketName, BucketManifestsPath);
            if (!Directory.Exists(manifestDir) || !Directory.GetFiles(manifestDir, "*.json").Any())
            {
                continue;
            }

            var files = Directory.GetFiles(manifestDir, "*.json");
            var manifests = new List<Manifest>(files.Length);
            foreach (var file in files)
            {
                await using var stream = System.IO.File.OpenRead(file);
                var manifest = await System.Text.Json.JsonSerializer.DeserializeAsync<Manifest>(stream);
                if (manifest is not null)
                {
                    manifests.Add(manifest);
                }
            }

            bucketLists.Add(new BucketListResponse(bucketName, manifests));
        }

        // TODO: use list cache.
        return Ok(new AllBucketsListResponse(bucketLists));
    }
}
