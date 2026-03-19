namespace CrawlerProtect;

/// <summary>
/// Configuration options for the CrawlerProtect TagHelper.
/// Register via <c>services.AddCrawlerProtect()</c> in <c>Program.cs</c>.
/// </summary>
public class CrawlerProtectOptions
{
    /// <summary>
    /// Default placeholder text shown before JavaScript decodes the content.
    /// Can be overridden per tag with the <c>placeholder</c> attribute.
    /// Defaults to <c>[Protected]</c>.
    /// </summary>
    public string DefaultPlaceholder { get; set; } = "[Protected]";

    /// <summary>
    /// Default link target used before JavaScript decodes the content.
    /// Can be overridden per tag with the <c>href</c> attribute.
    /// Defaults to <c>#</c>.
    /// </summary>
    public string DefaultLinkTarget { get; set; } = "#";
}
