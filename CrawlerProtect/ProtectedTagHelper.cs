using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Options;
using System.Text;

namespace CrawlerProtect;

/// <summary>
/// Converts <c>&lt;protected&gt;</c> tags to obfuscated <c>&lt;a&gt;</c> tags that bots cannot read.
/// <para>
/// Example input:
/// <code>&lt;protected class="protected-lnk"&gt;user@example.com&lt;/protected&gt;</code>
/// </para>
/// <para>
/// Example output (default options):
/// <code>&lt;a class="protected-lnk" data-protected="42*424f464645"&gt;[Protected]&lt;/a&gt;</code>
/// </para>
/// The companion script served by <c>app.MapCrawlerProtectDecoder()</c> reverses
/// the XOR encoding client-side for real users.
/// </summary>
[HtmlTargetElement("protected", TagStructure = TagStructure.NormalOrSelfClosing)]
public class ProtectedTagHelper : TagHelper
{
    /// <summary>
    /// Text shown in place of the protected content before JavaScript decodes it.
    /// Defaults to <see cref="CrawlerProtectOptions.DefaultPlaceholder"/>.
    /// </summary>
    [HtmlAttributeName("placeholder")]
    public string Placeholder { get; set; }

    /// <summary>
    /// Link target used before JavaScript decodes the content.
    /// Defaults to <see cref="CrawlerProtectOptions.DefaultLinkTarget"/>.
    /// </summary>
    [HtmlAttributeName("href")]
    public string LinkTarget { get; set; }

    private readonly CrawlerProtectOptions _options;

    public ProtectedTagHelper(IOptions<CrawlerProtectOptions> options)
    {
        _options    = options.Value;
        Placeholder = _options.DefaultPlaceholder;
        LinkTarget  = _options.DefaultLinkTarget;
    }

    /// <inheritdoc/>
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var content = (await output.GetChildContentAsync()).GetContent();
        var encoded = EncodeString(content, Random.Shared.Next(1, 256), _options);

        output.TagName = "a";
        output.Attributes.RemoveAll("placeholder");
        output.Attributes.SetAttribute(_options.DataAttribute, encoded);
        output.Content.SetContent(Placeholder);
    }

    /// <summary>
    /// XOR-encodes <paramref name="str"/> with <paramref name="key"/> using default options.
    /// Returns the result as <c>key*hexstring</c> (backward-compatible).
    /// </summary>
    /// <param name="str">The plaintext to encode.</param>
    /// <param name="key">XOR key in the range [1, 255].</param>
    public static string EncodeString(string str, int key)
        => EncodeString(str, key, new CrawlerProtectOptions());

    /// <summary>
    /// XOR-encodes <paramref name="str"/> with <paramref name="key"/> according to
    /// <paramref name="options"/>. The output format is governed by
    /// <see cref="CrawlerProtectOptions.Separator"/>,
    /// <see cref="CrawlerProtectOptions.KeyPosition"/>, and
    /// <see cref="CrawlerProtectOptions.PayloadEncoding"/>.
    /// </summary>
    /// <param name="str">The plaintext to encode.</param>
    /// <param name="key">XOR key in the range [1, 255].</param>
    /// <param name="options">Format options that control the output structure.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="key"/> is outside [1, 255].
    /// </exception>
    public static string EncodeString(string str, int key, CrawlerProtectOptions options)
    {
        if (key < 1 || key > 255)
            throw new ArgumentOutOfRangeException(nameof(key), "Key must be in the range [1, 255].");

        var bytes   = Encoding.UTF8.GetBytes(str);
        var payload = BuildPayload(bytes, key, options.PayloadEncoding);

        return options.KeyPosition == KeyPosition.Before
            ? $"{key}{options.Separator}{payload}"
            : $"{payload}{options.Separator}{key}";
    }

    private static string BuildPayload(byte[] bytes, int key, PayloadEncoding encoding)
    {
        if (encoding == PayloadEncoding.Base64)
        {
            var xored = new byte[bytes.Length];
            for (int i = 0; i < bytes.Length; i++)
                xored[i] = (byte)(bytes[i] ^ key);
            return Convert.ToBase64String(xored);
        }

        // Hex (default): each UTF-8 byte XORed with key → 2 lowercase hex digits
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
            sb.Append((b ^ key).ToString("x2"));
        return sb.ToString();
    }
}
