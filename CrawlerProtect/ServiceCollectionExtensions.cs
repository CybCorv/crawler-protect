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
    /// Registers CrawlerProtect configuration. Optional — the TagHelper works with
    /// default values without this call.
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

    /// <summary>
    /// Maps a GET endpoint that serves a JavaScript decoder generated from the
    /// current <see cref="CrawlerProtectOptions"/>. The script is built once at
    /// startup and cached; it contains the encoding format as baked-in literals
    /// so each deployment produces a unique decoder.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="path">
    /// URL path for the script. Defaults to <c>/crawler-protect.js</c>.
    /// </param>
    /// <example>
    /// <code>
    /// // Program.cs
    /// app.MapCrawlerProtectDecoder();
    ///
    /// // _Layout.cshtml
    /// &lt;script src="/crawler-protect.js"&gt;&lt;/script&gt;
    /// </code>
    /// </example>
    public static IEndpointRouteBuilder MapCrawlerProtectDecoder(
        this IEndpointRouteBuilder endpoints,
        string path = "/crawler-protect.js")
    {
        var options = endpoints.ServiceProvider
            .GetRequiredService<IOptions<CrawlerProtectOptions>>().Value;

        var js = CrawlerProtectJsGenerator.Generate(options);

        endpoints.MapGet(path, () => Results.Content(js, "application/javascript"));

        return endpoints;
    }
}
