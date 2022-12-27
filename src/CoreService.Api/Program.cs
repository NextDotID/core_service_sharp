using Chubrik.Json;
using CoreService.Api.Agents;
using CoreService.Api.Injectors;
using CoreService.Api.Persistences;
using CoreService.Api.Vaults;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .Configure<RouteOptions>(options => options.LowercaseUrls = true)
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicies.SnakeLowerCase);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

builder.Services.AddPersistence();
builder.Services.AddTransient<IVault, PersistenceVault>();
builder.Services.AddTransient<IAgent, DockerComposeAgent>();
builder.Services.AddTransient<Injector>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
