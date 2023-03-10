using CoreService.Api.Agents;
using CoreService.Api.Database;
using CoreService.Api.Injectors;
using CoreService.Api.Vaults;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .Configure<RouteOptions>(options => options.LowercaseUrls = true)
    .AddCors(options => options.AddDefaultPolicy(
        policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()))
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var apiXml = $"{nameof(CoreService)}.{nameof(CoreService.Api)}.xml";
    var sharedXml = $"{nameof(CoreService)}.{nameof(CoreService.Shared)}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, apiXml));
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, sharedXml));
});
builder.Services.AddProblemDetails();

builder.Services.AddLiteDb();
builder.Services.AddHttpClient();
builder.Services.AddTransient<IVault, DatabaseVault>();
builder.Services.AddTransient<IAgent, DockerComposeAgent>();
builder.Services.AddTransient<Injector>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
