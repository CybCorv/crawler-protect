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

    /// <summary>
    /// Character used to separate the key from the payload in the encoded value.
    /// Choose a character that cannot appear in the encoded payload to avoid ambiguity
    /// (hex payload: safe chars are anything outside <c>[0-9a-f]</c>;
    ///  base-64 payload: safe chars are anything outside <c>[A-Za-z0-9+/=]</c>).
    /// Defaults to <c>*</c>.
    /// </summary>
    public char Separator { get; set; } = '*';

    /// <summary>
    /// Whether the XOR key is placed before or after the encoded payload.
    /// Defaults to <see cref="CrawlerProtect.KeyPosition.Before"/>.
    /// </summary>
    public KeyPosition KeyPosition { get; set; } = KeyPosition.Before;

    /// <summary>
    /// Encoding applied to the XOR-encrypted payload bytes.
    /// Defaults to <see cref="CrawlerProtect.PayloadEncoding.Hex"/>.
    /// </summary>
    public PayloadEncoding PayloadEncoding { get; set; } = PayloadEncoding.Hex;

    /// <summary>
    /// HTML <c>data-*</c> attribute used to carry the encoded value on the generated element.
    /// Must start with <c>data-</c>. Defaults to <c>data-protected</c>.
    /// </summary>
    public string DataAttribute { get; set; } = "data-protected";
}
