using CoreService.Api.Agents;
using CoreService.Api.Database;
using CoreService.Api.Injectors;
using CoreService.Api.Vaults;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .Configure<RouteOptions>(options => options.LowercaseUrls = true)
    .AddControllersWithViews()
    .ConfigureApplicationPartManager(o =>
    {
        o.ApplicationParts.Clear();
        o.ApplicationParts.Add(new AssemblyPart(typeof(CoreService.Api.Controllers.CoreController).Assembly));
    })
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);

builder.Services.AddRazorPages();
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
app.UseAuthorization();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
