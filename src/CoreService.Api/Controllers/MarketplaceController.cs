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
    private const string BucketDefaultBranch = "main";
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
    [HttpGet("list/{bucketName}")]
    public async ValueTask<ActionResult<BucketListResponse>> ListAsync(string bucketName)
    {
        var bucketCol = liteDatabase.GetCollection<Bucket>();
        var bucket = bucketCol.FindOne(b => b.Name == bucketName);
        if (bucket is null)
        {
            return Problem("Bucket with the name does not exist.", null, StatusCodes.Status404NotFound);
        }

        var manifestDir = Path.Combine(bucketDirectory, bucket.Name, BucketManifestsPath);
        if (!Directory.Exists(manifestDir) || !Directory.GetFiles(manifestDir, "*.json").Any())
        {
            return Problem("Bucket with the name does not exist.", null, StatusCodes.Status404NotFound);
        }

        BucketListResponse response;
        try
        {
            response = await ListBucketAsync(bucket);
        }
        catch (Exception ex)
        {
            logger.MarketplacePullFailed(bucketName, ex);
            return Problem("Failed to list all applications in the bucket.", bucketName, StatusCodes.Status500InternalServerError);
        }

        return Ok(response);
    }

    /// <summary>
    ///     List applications from all buckets.
    /// </summary>
    /// <returns>Ok.</returns>
    /// <response code="200">Application list from all buckets.</response>
    /// <response code="404">If there are no valid buckets.</response>
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
        foreach (var bucket in buckets)
        {
            BucketListResponse response;
            try
            {
                response = await ListBucketAsync(bucket);
            }
            catch (Exception ex)
            {
                logger.MarketplacePullFailed(bucket.Name, ex);
                return Problem("Failed to list all applications in the bucket.", bucket.Name, StatusCodes.Status500InternalServerError);
            }

            bucketLists.Add(response);
        }

        return Ok(new AllBucketsListResponse(bucketLists));
    }

    /// <summary>
    ///     Pull/update a bucket.
    /// </summary>
    /// <param name="bucketName">Bucket name.</param>
    /// <returns>Ok.</returns>
    /// <response code="200">If pulled correctly.</response>
    /// <response code="404">If there are no valid buckets.</response>
    [HttpPost("pull/{bucketName}")]
    public async ValueTask<ActionResult> PullAsync(string bucketName)
    {
        var bucketCol = liteDatabase.GetCollection<Bucket>();
        var bucket = bucketCol.FindOne(b => b.Name == bucketName);
        if (bucket is null)
        {
            return Problem("Bucket with the name does not exist.", null, StatusCodes.Status404NotFound);
        }

        try
        {
            await PullBucketAsync(bucket);
        }
        catch (Exception ex)
        {
            logger.MarketplacePullFailed(bucket.Name, ex);
            return Problem("Failed to pull repository.", null, StatusCodes.Status500InternalServerError);
        }

        bucketCol.Update(bucket);
        return Ok();
    }

    /// <summary>
    ///     Pull/update all buckets.
    /// </summary>
    /// <returns>Ok (empty), or error messages while pulling if at least one bucket were not pulled.</returns>
    /// <response code="200">If at least one bucket was pulled.</response>
    /// <response code="404">If there are no valid buckets.</response>
    /// <response code="500">If all buckets were not pulled correctly.</response>
    [HttpPost("pull")]
    public async ValueTask<ActionResult<ProblemDetails>> PullAllAsync()
    {
        var bucketCol = liteDatabase.GetCollection<Bucket>();
        var buckets = bucketCol.FindAll().ToList();
        if (!buckets.Any())
        {
            return Problem("No valid buckets.", null, StatusCodes.Status404NotFound);
        }

        // <Bucket Name, Error messages>.
        var errors = new Dictionary<string, string>();
        foreach (var bucket in buckets)
        {
            try
            {
                await PullBucketAsync(bucket);
            }
            catch (Exception ex)
            {
                logger.MarketplacePullFailed(bucket.Name, ex);
                errors[bucket.Name] = ex.Message;
            }
        }

        var problem = new ProblemDetails
        {
            Detail = $"{buckets.Count} bucket(s) were not pulled correctly.",
            Title = "Error occurred while pulling buckets.",
            Status = StatusCodes.Status200OK,
        };
        problem.Extensions["errors"] = errors;

        // Only 500 if all buckets were not pulled.
        if (errors.Count >= buckets.Count)
        {
            problem.Status = StatusCodes.Status500InternalServerError;
            return StatusCode(StatusCodes.Status500InternalServerError, problem);
        }

        return errors.Any() ? Ok(problem) : Ok();
    }

    private async ValueTask<BucketListResponse> ListBucketAsync(Bucket bucket)
    {
        var manifestDir = Path.Combine(bucketDirectory, bucket.Name, BucketManifestsPath);
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

        return new BucketListResponse(bucket.Name, manifests);
    }

    private ValueTask PullBucketAsync(Bucket bucket)
    {
        var now = DateTimeOffset.Now;
        var repoPath = Path.Combine(bucketDirectory, bucket.Name);
        var mergerSig = new Signature(nameof(CoreService), $"{nameof(CoreService)}@nextid", now);

        using var repo = new Repository(repoPath);
        _ = Commands.Checkout(repo, repo.Branches[BucketDefaultBranch]);

        var remote = repo.Network.Remotes["origin"];
        var resSpecs = remote.FetchRefSpecs.Select(s => s.Specification);
        Commands.Fetch(repo, remote.Name, resSpecs, new FetchOptions(), string.Empty);

        var res = Commands.Pull(repo, mergerSig, new PullOptions());
        if (res.Status != MergeStatus.UpToDate)
        {
            throw new InvalidOperationException("Repository is not up to date.");
        }

        bucket.PulledAt = now;
        return ValueTask.CompletedTask;
    }
}
