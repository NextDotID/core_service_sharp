using System.Reflection;
using CoreService.Api.Agents;
using CoreService.Api.Database;
using CoreService.Api.Injectors;
using CoreService.Api.Vaults;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .Configure<RouteOptions>(options => options.LowercaseUrls = true)
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
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
app.UseAuthorization();
app.MapControllers();

app.Run();
