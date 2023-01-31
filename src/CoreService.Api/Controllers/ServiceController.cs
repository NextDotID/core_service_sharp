namespace CoreService.Api.Controllers;

using System.IO.Compression;
using CoreService.Api.Agents;
using CoreService.Api.Injectors;
using CoreService.Api.Persistences;
using CoreService.Api.Vaults;
using CoreService.Shared.Injectors;
using CoreService.Shared.Payloads;
using FluentResults;
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

    [HttpGet]
    public async ValueTask<ActionResult<ServicesResponse>> GetAllAsync()
    {
        var list = await persistence.ListAsync();
        if (list.IsFailed)
        {
            return Problem(string.Join(',', list.Errors));
        }

        var result = new List<ServiceStatus>();
        foreach (var svc in list.Value)
        {
            var running = await agent.IsRunningAsync(svc);
            result.Add(new ServiceStatus(svc, running.ValueOrDefault));
        }

        return new ServicesResponse(result);
    }

    [HttpPost("{service}/prepare")]
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
            var textRes = await persistence.ReadTextAsync(service, filename);
            prompts.AddRange(injector.Extract(textRes.Value).ValueOrDefault);
        }

        return new PrepareResponse(prompts.Distinct());
    }

    [HttpPost("{service}/create")]
    public async ValueTask<ActionResult> CreateAsync(string service, [FromBody] CreatePayload payload)
    {
        var internals = await vault.LoadInternalAsync();
        var hostDir = await persistence.GetAbsolutePathAsync(service);
        var files = await persistence.ListAsync(service);

        if (internals.IsFailed || hostDir.IsFailed || files.IsFailed)
        {
            return Problem(string.Join(',', Result.Merge(internals, hostDir, files).Errors));
        }

        foreach (var filename in files.Value)
        {
            var content = await persistence.ReadTextAsync(service, filename);
            var output = injector.Inject(content.Value, internals.Value, payload.Prompts).Value;
            output = output
                .Replace("{{INTERNAL:DIRECTORY}}", hostDir.Value)
                .Replace("{{INTERNAL:SUBDOMAIN}}", $"{service}.{internals.Value.Host.Domain}");

            if (injector.Validate(output))
            {
                await persistence.WriteTextAsync(service, filename, output);
            }
            else
            {
                return Problem("injection points still presented");
            }
        }

        await agent.StartAsync(service);
        return Ok();
    }
}
