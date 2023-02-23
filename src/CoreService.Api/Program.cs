using System.Reflection;
using CoreService.Api.Agents;
using CoreService.Api.Database;
using CoreService.Api.Injectors;
using CoreService.Api.Vaults;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .Configure<RouteOptions>(options => options.LowercaseUrls = true)
    .AddControllersWithViews()
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);

builder.Services.AddHttpClient();
builder.Services.AddRazorPages();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

builder.Services.AddLiteDb();
builder.Services.AddTransient<IVault, DatabaseVault>();
builder.Services.AddTransient<IAgent, DockerComposeAgent>();
builder.Services.AddTransient<Injector>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseWebAssemblyDebugging();
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
