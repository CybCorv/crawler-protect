using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text;

namespace CrawlerProtect;

/// <summary>
/// Converts <c>&lt;protected&gt;</c> tags to obfuscated <c>&lt;a&gt;</c> tags that bots cannot read.
/// <para>
/// Example input:
/// <code>&lt;protected class="protected-lnk"&gt;user@example.com&lt;/protected&gt;</code>
/// </para>
/// <para>
/// Example output:
/// <code>&lt;a class="protected-lnk" data-protected="42*424f464645"&gt;[Protected]&lt;/a&gt;</code>
/// </para>
/// The companion <c>decode.js</c> reverses the XOR encoding client-side for real users.
/// </summary>
[HtmlTargetElement("protected", TagStructure = TagStructure.NormalOrSelfClosing)]
public class ProtectedTagHelper : TagHelper
{
    /// <summary>
    /// Text shown in place of the protected content before JavaScript decodes it.
    /// Defaults to <c>[Protected]</c>.
    /// </summary>
    [HtmlAttributeName("placeholder")]
    public string Placeholder { get; set; } = "[Protected]";

    /// <summary>
    /// Link target used before JavaScript decodes the content.
    /// Defaults to <c>#</c>.
    /// </summary>
    [HtmlAttributeName("href")]
    public string LinkTarget { get; set; } = "#";

    /// <inheritdoc/>
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var content = (await output.GetChildContentAsync()).GetContent();
        var encoded = EncodeString(content, Random.Shared.Next(1, 256));

        output.TagName = "a";
        output.Attributes.RemoveAll("placeholder");
        output.Attributes.SetAttribute("data-protected", encoded);
        output.Content.SetContent(Placeholder);
    }

    /// <summary>
    /// XOR-encodes <paramref name="str"/> with <paramref name="key"/> and returns the result
    /// as <c>key*hexstring</c>.
    /// </summary>
    /// <param name="str">The plaintext to encode.</param>
    /// <param name="key">XOR key in the range [1, 255].</param>
    /// <returns>Encoded string in the format <c>key*hexstring</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="key"/> is outside [1, 255].
    /// </exception>
    public static string EncodeString(string str, int key)
    {
        if (key < 1 || key > 255)
            throw new ArgumentOutOfRangeException(nameof(key), "Key must be in the range [1, 255].");

        var bytes = Encoding.UTF8.GetBytes(str);
        var encrypted = new StringBuilder(bytes.Length * 2 + 4);

        foreach (var b in bytes)
        {
            // Each UTF-8 byte is in [0, 255], so XOR with key ∈ [1, 255]
            // always yields a value in [0, 255] → exactly 2 hex digits.
            encrypted.Append((b ^ key).ToString("x2"));
        }

        return key + "*" + encrypted;
    }
}
