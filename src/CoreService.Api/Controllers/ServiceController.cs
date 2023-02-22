namespace CoreService.Api.Controllers;

using System.IO.Compression;
using CoreService.Api.Agents;
using CoreService.Api.Injectors;
using CoreService.Api.Persistences;
using CoreService.Api.Vaults;
using CoreService.Shared.Injectors;
using CoreService.Shared.Payloads;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ServiceController : ControllerBase
{
    private readonly Injector injector;
    private readonly IAgent agent;
    private readonly IPersistence persistence;
    private readonly IVault vault;
    private readonly HttpClient httpClient;
    private readonly ILogger logger;

    public ServiceController(
        Injector injector,
        IAgent agent,
        IPersistence persistence,
        IVault vault,
        IHttpClientFactory httpClientFactory,
        ILogger<ServiceController> logger)
    {
        this.injector = injector;
        this.agent = agent;
        this.persistence = persistence;
        this.vault = vault;
        this.httpClient = httpClientFactory.CreateClient();
        this.logger = logger;
    }

    /// <summary>
    ///     Get a list of all services managed by CoreService.
    /// </summary>
    /// <returns>A services list.</returns>
    /// <response code="200"></response>
    [HttpGet(Name = "Get all services")]
    public async ValueTask<ActionResult<ServicesResponse>> GetAllAsync()
    {
        var list = await agent.ListAsync();
        return new ServicesResponse(list.Select(s => new ServiceStatus(s.Key, s.Value)));
    }

    /// <summary>
    ///     Prepare a service creation.
    /// </summary>
    /// <param name="service">Service name.</param>
    /// <param name="payload">Manifest URL.</param>
    /// <returns>A list of injection points to be filled.</returns>
    /// <remarks>
    ///     In the response of this API, there are 3 **types** of injection points.
    ///     The user only needs to input `PromptPoint` only.
    /// </remarks>
    /// <response code="200">If prepared.</response>
    [HttpPost("{service}/prepare", Name = "Prepare a service")]
    public async ValueTask<ActionResult<PrepareResponse>> PrepareAsync(string service, [FromBody] PreparePayload payload)
    {
        var prompts = new List<InjectionPoint>();
        await using var ms = new MemoryStream();
        await using (var stream = await httpClient.GetStreamAsync(payload.Source))
        {
            await stream.CopyToAsync(ms);
        }

        using var zip = new ZipArchive(ms, ZipArchiveMode.Read);

        // Junk path.
        var parent = zip.Entries.FirstOrDefault()?.FullName;
        var junkParent = !string.IsNullOrEmpty(parent)
                && Path.EndsInDirectorySeparator(parent)
                && zip.Entries.All(e => e.FullName.StartsWith(parent, StringComparison.OrdinalIgnoreCase));

        foreach (var entry in zip.Entries.Where(e => !Path.EndsInDirectorySeparator(e.FullName)))
        {
            await using var entryStream = entry.Open();
            var filename = junkParent ? entry.FullName[(parent?.Length ?? 0)..] : entry.FullName;
            await persistence.WriteAsync(service, filename, entryStream);
            var text = await persistence.ReadTextAsync(service, filename);
            prompts.AddRange(injector.Extract(text));
        }

        return new PrepareResponse(prompts.Distinct());
    }

    /// <summary>
    ///     Create the service with the given prompts.
    /// </summary>
    /// <param name="service">Service name.</param>
    /// <param name="payload">Prompt values and other configuration.</param>
    /// <returns>Ok.</returns>
    /// <response code="200">If created.</response>
    /// <response code="400">If some injection points are still presented.</response>
    [HttpPost("{service}/create", Name = "Create and start a service")]
    public async ValueTask<ActionResult> CreateAsync(string service, [FromBody] CreatePayload payload)
    {
        var internals = await vault.LoadInternalAsync();
        var hostDir = await persistence.GetPathAsync(service);
        var files = await persistence.ListAsync(service);

        foreach (var filename in files)
        {
            var content = await persistence.ReadTextAsync(service, filename);
            var output = injector.Inject(content, internals, payload.Prompts)
                .Replace("{{INTERNAL:DIRECTORY}}", hostDir)
                .Replace("{{INTERNAL:SUBDOMAIN}}", $"{service}.{internals.Host.Domain}");

            if (injector.Validate(output))
            {
                await persistence.WriteTextAsync(service, filename, output);
            }
            else
            {
                return Problem("Injection points still presented.");
            }
        }

        await agent.UpAsync(service);
        return Ok();
    }
}
