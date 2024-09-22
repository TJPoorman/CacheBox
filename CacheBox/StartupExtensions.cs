using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CacheBox;

public static class StartupExtensions
{
    /// <summary>
    /// Adds a caching provider to the application's dependency injection container.
    /// </summary>
    /// <typeparam name="TProvider">The type of the caching provider to add, which must implement <see cref="ICacheProvider"/>.</typeparam>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> instance to which the caching provider is added.</param>
    /// <returns>The updated <see cref="IHostApplicationBuilder"/> instance.</returns>
    /// <remarks>
    /// This method registers the specified caching provider type <typeparamref name="TProvider"/> as a singleton in the service collection.
    /// </remarks>
    public static IHostApplicationBuilder AddCacheProvider<TProvider>(this IHostApplicationBuilder builder)
        where TProvider : class, ICacheProvider
    {
        ILogger logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<TProvider>>();
        IConfigurationSection config = builder.Configuration.GetSection("Cache");
        if (!config.Exists())
        {
            logger.LogWarning("Cache configuration missing. Caching disabled.");
            return builder;
        }
        if (!typeof(TProvider).Name.Equals("MemoryCacheProvider", StringComparison.InvariantCultureIgnoreCase) && !config.GetSection("ConnectionString").Exists())
        {
            logger.LogWarning("Cache configuration missing ConnectionString. Caching disabled.");
        }

        logger.LogInformation("Cache configuration attempting to connect to {Provider} provider", typeof(TProvider).Name);
        builder.Services.AddSingleton<ICacheProvider, TProvider>();

        try
        {
            var tempProvider = builder.Services.BuildServiceProvider().GetRequiredService<ICacheProvider>();
            if (!tempProvider.IsConnected) throw new Exception($"{tempProvider.GetType().Name} is not connected.");
            logger.LogInformation("Cache configuration successfully initialized provider.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cache configuration failed to initialize provider.");
        }

        return builder;
    }

    internal static string IfNotNull(this string input, string output) => input is null ? string.Empty : output;
}
