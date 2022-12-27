namespace CoreService.Api.Persistences;
public static class PersistenceExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        return services.AddTransient<IPersistence>(provider =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            var logger = provider.GetRequiredService<ILogger<FilePersistence>>();
            return new FilePersistence(config["Persistence:Root"]!, config["Persistence:Host"]!, logger);
        });
    }
}
