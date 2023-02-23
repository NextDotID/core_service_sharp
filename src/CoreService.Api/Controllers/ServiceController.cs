namespace CoreService.Api.Controllers;

using CoreService.Api.Agents;
using CoreService.Api.Injectors;
using CoreService.Api.Logging;
using CoreService.Api.Vaults;
using CoreService.Shared.Models;
using CoreService.Shared.Payloads;
using LiteDB;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ServiceController : ControllerBase
{
    private readonly Injector injector;
    private readonly ILiteDatabase liteDatabase;
    private readonly IAgent agent;
    private readonly IVault vault;
    private readonly HttpClient httpClient;
    private readonly ILogger logger;

    public ServiceController(
        Injector injector,
        ILiteDatabase liteDatabase,
        IAgent agent,
        IVault vault,
        IHttpClientFactory httpClientFactory,
        ILogger<ServiceController> logger)
    {
        this.injector = injector;
        this.liteDatabase = liteDatabase;
        this.agent = agent;
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
        var svcColl = liteDatabase.GetCollection<Service>();
        var services = svcColl.FindAll();
        var list = await agent.ListAsync();

        // Intersect docker-compose services with the services managed by CoreService (in db).
        var status = list.Where(p => services.Any(s => s.Name == p.Key))
            .Select(p => new ServiceStatus(p.Key, p.Value, Array.Empty<string>()));
        return new ServicesResponse(status);
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
    /// <response code="409">If a service with the same name is already created.</response>
    [HttpPost("{service}/prepare", Name = "Prepare a service")]
    public async ValueTask<ActionResult<PrepareResponse>> PrepareAsync(string service, [FromBody] PreparePayload payload)
    {
        var svcColl = liteDatabase.GetCollection<Service>();
        var svc = svcColl.FindOne(s => s.Name == service) ?? new Service { Name = service };
        if (svc.IsCreated)
        {
            return Problem("A service with the same name is already created.", null, StatusCodes.Status409Conflict);
        }

        var composeRaw = await httpClient.GetStringAsync(payload.Source);
        var points = injector.Extract(composeRaw).Distinct().ToArray();
        svc.Compose = composeRaw;

        liteDatabase.GetCollection<Service>().Upsert(svc);
        return new PrepareResponse(points);
    }

    /// <summary>
    ///     Create the service with the given prompts.
    /// </summary>
    /// <param name="service">Service name.</param>
    /// <param name="payload">Prompt values and other configuration.</param>
    /// <returns>Ok.</returns>
    /// <response code="200">If created.</response>
    /// <response code="400">If some injection points are still presented.</response>
    /// <response code="404">If a service with this name is not found.</response>
    [HttpPost("{service}/create", Name = "Create and start a service")]
    public async ValueTask<ActionResult> CreateAsync(string service, [FromBody] CreatePayload payload)
    {
        var svcColl = liteDatabase.GetCollection<Service>();
        var svc = svcColl.FindOne(s => s.Name == service);
        if (svc == null)
        {
            return Problem("Service is not found.", null, StatusCodes.Status404NotFound);
        }

        var internals = await vault.LoadInternalAsync();
        var injected = injector.Inject(svc.Compose, internals, payload.Prompts);

        if (!injector.Validate(injected, out var point))
        {
            return Problem("Injection points still presented.", point, StatusCodes.Status400BadRequest);
        }

        svc.Compose = injected;
        svc.Prompts = payload.Prompts;

        try
        {
            await agent.UpAsync(service, injected);
            svc.IsCreated = true;
            svcColl.Update(svc);
        }
        catch (Exception ex)
        {
            logger.DockerInteractionFailed(service, ex.Message, ex);
            return Problem("docker-compose failed to up", null, StatusCodes.Status500InternalServerError);
        }

        return Ok();
    }
}
