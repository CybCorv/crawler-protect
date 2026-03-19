using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CrawlerProtect;

/// <summary>
/// Extension methods for registering and mapping CrawlerProtect services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers CrawlerProtect configuration and startup validation.
    /// Optional — the TagHelper works with default values without this call,
    /// but validation only runs when this method is called.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.Services.AddCrawlerProtect(options =>
    /// {
    ///     options.DefaultPlaceholder = "...";
    ///     options.Separator          = '|';
    ///     options.KeyPosition        = KeyPosition.After;
    ///     options.PayloadEncoding    = PayloadEncoding.Base64;
    ///     options.DataAttribute      = "data-enc";
    ///     options.ScriptPath         = "/assets/cp.js";
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

        // Validate options eagerly at startup so misconfiguration surfaces
        // immediately rather than at the first request.
        services.AddSingleton<IValidateOptions<CrawlerProtectOptions>,
                              CrawlerProtectOptionsValidator>();

        return services;
    }

    /// <summary>
    /// Maps a GET endpoint that serves a JavaScript decoder generated from the
    /// current <see cref="CrawlerProtectOptions"/>. The script is built once at
    /// startup with all format parameters baked in as literals.
    /// </summary>
    /// <remarks>
    /// The endpoint path is taken from <see cref="CrawlerProtectOptions.ScriptPath"/>
    /// (default <c>/crawler-protect.js</c>).
    /// </remarks>
    /// <example>
    /// <code>
    /// // Program.cs
    /// app.MapCrawlerProtectDecoder();
    ///
    /// // _Layout.cshtml — path matches CrawlerProtectOptions.ScriptPath
    /// &lt;script src="/crawler-protect.js"&gt;&lt;/script&gt;
    /// </code>
    /// </example>
    public static IEndpointRouteBuilder MapCrawlerProtectDecoder(
        this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider
            .GetRequiredService<IOptions<CrawlerProtectOptions>>().Value;

        var js = CrawlerProtectJsGenerator.Generate(options);

        endpoints.MapGet(options.ScriptPath, () => Results.Content(js, "application/javascript"));

        return endpoints;
    }
}
