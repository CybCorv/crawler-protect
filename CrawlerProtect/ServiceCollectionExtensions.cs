using Microsoft.Extensions.DependencyInjection;

namespace CrawlerProtect;

/// <summary>
/// Extension methods for registering CrawlerProtect services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers CrawlerProtect configuration. Optional — the TagHelper works with
    /// default values without this call.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.Services.AddCrawlerProtect(options =>
    /// {
    ///     options.DefaultPlaceholder = "...";
    ///     options.DefaultLinkTarget  = "#";
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddCrawlerProtect(
        this IServiceCollection services,
        Action<CrawlerProtectOptions>? configure = null)
    {
        if (configure is null)
            services.AddOptions<CrawlerProtectOptions>();
        else
            services.Configure<CrawlerProtectOptions>(configure);

        return services;
    }
}
