namespace CoreService.Api.Database;
using LiteDB;

public static class LiteDbExtensions
{
    public static IServiceCollection AddLiteDb(this IServiceCollection services)
    {
        return services.AddSingleton<ILiteDatabase>(provider =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            if (string.IsNullOrEmpty(config["Database:Path"]))
            {
                throw new ArgumentException("`Database:Path` must be configured.");
            }

            // Open or create a database in that location.
            return new LiteDatabase(config["Database:Path"]);
        });
    }
}
