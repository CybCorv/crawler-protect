using System.Text;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Options;
using Xunit;

namespace CrawlerProtect.Tests;

public class EncodeStringTests
{
    // ── Default options (backward-compat) ────────────────────────────────

    [Fact]
    public void EncodeString_DefaultOptions_ReturnsKeyStarHex()
    {
        var result = ProtectedTagHelper.EncodeString("hello", 42);

        Assert.Matches(@"^\d+\*[0-9a-f]+$", result);
    }

    [Fact]
    public void EncodeString_DefaultOptions_PrefixMatchesKey()
    {
        var result = ProtectedTagHelper.EncodeString("hello", 42);

        Assert.StartsWith("42*", result);
    }

    [Theory]
    [InlineData("test@example.com", 42)]
    [InlineData("hello world", 1)]
    [InlineData("sensitive data", 255)]
    // Latin Extended (U+0080–U+00FF) — 2-byte UTF-8
    [InlineData("café", 100)]
    // Beyond Latin-1 (> U+00FF)
    [InlineData("Ω résumé €", 50)]
    [InlineData("中文 日本語", 77)]
    [InlineData("emoji 🎉", 128)]
    public void EncodeString_DefaultOptions_IsSymmetricWithXor(string input, int key)
    {
        var encoded = ProtectedTagHelper.EncodeString(input, key);

        var sep    = encoded.IndexOf('*');
        var hexStr = encoded[(sep + 1)..];

        var bytes = new byte[hexStr.Length / 2];
        for (int i = 0; i < hexStr.Length; i += 2)
            bytes[i / 2] = (byte)(Convert.ToInt32(hexStr.Substring(i, 2), 16) ^ key);

        Assert.Equal(input, Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public void EncodeString_EmptyInput_ReturnsKeyWithNoPayload()
    {
        var result = ProtectedTagHelper.EncodeString("", 10);

        Assert.Equal("10*", result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(128)]
    [InlineData(255)]
    public void EncodeString_BoundaryKeys_AreAccepted(int key)
    {
        var result = ProtectedTagHelper.EncodeString("test", key);

        Assert.StartsWith($"{key}*", result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(256)]
    [InlineData(-1)]
    public void EncodeString_OutOfRangeKey_Throws(int key)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ProtectedTagHelper.EncodeString("test", key));
    }

    [Fact]
    public void EncodeString_DifferentKeysProduceDifferentOutput()
    {
        var result1 = ProtectedTagHelper.EncodeString("hello", 10);
        var result2 = ProtectedTagHelper.EncodeString("hello", 20);

        var hex1 = result1[(result1.IndexOf('*') + 1)..];
        var hex2 = result2[(result2.IndexOf('*') + 1)..];
        Assert.NotEqual(hex1, hex2);
    }

    [Fact]
    public void EncodeString_HexPayloadLengthIsTwiceUtf8ByteCount()
    {
        var input  = "hello";
        var result = ProtectedTagHelper.EncodeString(input, 42);

        var hex = result[(result.IndexOf('*') + 1)..];
        Assert.Equal(Encoding.UTF8.GetByteCount(input) * 2, hex.Length);
    }

    // ── Custom separator ─────────────────────────────────────────────────

    [Fact]
    public void EncodeString_CustomSeparator_UsedInOutput()
    {
        var opts   = new CrawlerProtectOptions { Separator = '|' };
        var result = ProtectedTagHelper.EncodeString("hello", 42, opts);

        Assert.Contains("|", result);
        Assert.DoesNotContain("*", result);
        Assert.StartsWith("42|", result);
    }

    // ── Key position ─────────────────────────────────────────────────────

    [Fact]
    public void EncodeString_KeyAfter_KeyAppearsAtEnd()
    {
        var opts   = new CrawlerProtectOptions { KeyPosition = KeyPosition.After };
        var result = ProtectedTagHelper.EncodeString("hello", 42, opts);

        Assert.EndsWith("*42", result);
        Assert.Matches(@"^[0-9a-f]+\*42$", result);
    }

    [Fact]
    public void EncodeString_KeyAfter_IsSymmetricWithXor()
    {
        const string input = "hello";
        const int    key   = 77;
        var opts = new CrawlerProtectOptions { KeyPosition = KeyPosition.After };

        var encoded = ProtectedTagHelper.EncodeString(input, key, opts);

        var si     = encoded.LastIndexOf('*');
        var hexStr = encoded[..si];
        var parsedKey = int.Parse(encoded[(si + 1)..]);

        Assert.Equal(key, parsedKey);

        var bytes = new byte[hexStr.Length / 2];
        for (int i = 0; i < hexStr.Length; i += 2)
            bytes[i / 2] = (byte)(Convert.ToInt32(hexStr.Substring(i, 2), 16) ^ parsedKey);

        Assert.Equal(input, Encoding.UTF8.GetString(bytes));
    }

    // ── Base-64 encoding ─────────────────────────────────────────────────

    [Fact]
    public void EncodeString_Base64_PayloadIsValidBase64()
    {
        var opts   = new CrawlerProtectOptions { PayloadEncoding = PayloadEncoding.Base64 };
        var result = ProtectedTagHelper.EncodeString("hello", 42, opts);

        var sep     = result.IndexOf('*');
        var payload = result[(sep + 1)..];

        // Should not throw
        var decoded = Convert.FromBase64String(payload);
        Assert.Equal(5, decoded.Length); // "hello" = 5 bytes
    }

    [Theory]
    [InlineData("test@example.com", 42)]
    [InlineData("café", 100)]
    [InlineData("emoji 🎉", 128)]
    public void EncodeString_Base64_IsSymmetricWithXor(string input, int key)
    {
        var opts    = new CrawlerProtectOptions { PayloadEncoding = PayloadEncoding.Base64 };
        var encoded = ProtectedTagHelper.EncodeString(input, key, opts);

        var sep     = encoded.IndexOf('*');
        var parsedKey = int.Parse(encoded[..sep]);
        var xored   = Convert.FromBase64String(encoded[(sep + 1)..]);

        var original = new byte[xored.Length];
        for (int i = 0; i < xored.Length; i++)
            original[i] = (byte)(xored[i] ^ parsedKey);

        Assert.Equal(input, Encoding.UTF8.GetString(original));
    }

    // ── Combined options ─────────────────────────────────────────────────

    [Fact]
    public void EncodeString_Base64KeyAfterCustomSep_RoundTrip()
    {
        const string input = "secret@example.com";
        const int    key   = 99;
        var opts = new CrawlerProtectOptions
        {
            Separator       = '|',
            KeyPosition     = KeyPosition.After,
            PayloadEncoding = PayloadEncoding.Base64,
        };

        var encoded = ProtectedTagHelper.EncodeString(input, key, opts);

        // Format: base64payload|key
        var si      = encoded.LastIndexOf('|');
        var payload = encoded[..si];
        var parsedKey = int.Parse(encoded[(si + 1)..]);

        Assert.Equal(key, parsedKey);

        var xored    = Convert.FromBase64String(payload);
        var original = new byte[xored.Length];
        for (int i = 0; i < xored.Length; i++)
            original[i] = (byte)(xored[i] ^ parsedKey);

        Assert.Equal(input, Encoding.UTF8.GetString(original));
    }
}

public class ProtectedTagHelperProcessTests
{
    private static readonly IOptions<CrawlerProtectOptions> DefaultOptions =
        Options.Create(new CrawlerProtectOptions());

    private static TagHelperContext MakeContext() =>
        new(
            tagName: "protected",
            allAttributes: new TagHelperAttributeList(),
            items: new Dictionary<object, object>(),
            uniqueId: "test-id"
        );

    private static TagHelperOutput MakeOutput(string childContent, TagHelperAttributeList? attributes = null)
    {
        attributes ??= new TagHelperAttributeList();
        return new TagHelperOutput(
            tagName: "protected",
            attributes: attributes,
            getChildContentAsync: (_, _) =>
            {
                var content = new DefaultTagHelperContent();
                content.SetContent(childContent);
                return Task.FromResult<TagHelperContent>(content);
            });
    }

    [Fact]
    public async Task ProcessAsync_ChangesTagNameToAnchor()
    {
        var helper = new ProtectedTagHelper(DefaultOptions);
        var output = MakeOutput("user@example.com");

        await helper.ProcessAsync(MakeContext(), output);

        Assert.Equal("a", output.TagName);
    }

    [Fact]
    public async Task ProcessAsync_SetsDataProtectedAttribute()
    {
        var helper = new ProtectedTagHelper(DefaultOptions);
        var output = MakeOutput("user@example.com");

        await helper.ProcessAsync(MakeContext(), output);

        Assert.True(output.Attributes.ContainsName("data-protected"));
        var attr = output.Attributes["data-protected"].Value.ToString()!;
        Assert.Matches(@"^\d+\*[0-9a-f]+$", attr);
    }

    [Fact]
    public async Task ProcessAsync_CustomDataAttribute_UsesConfiguredName()
    {
        var opts   = Options.Create(new CrawlerProtectOptions { DataAttribute = "data-enc" });
        var helper = new ProtectedTagHelper(opts);
        var output = MakeOutput("user@example.com");

        await helper.ProcessAsync(MakeContext(), output);

        Assert.True(output.Attributes.ContainsName("data-enc"));
        Assert.False(output.Attributes.ContainsName("data-protected"));
    }

    [Fact]
    public async Task ProcessAsync_ContentIsPlaceholder()
    {
        var helper = new ProtectedTagHelper(DefaultOptions) { Placeholder = "[Email]" };
        var output = MakeOutput("user@example.com");

        await helper.ProcessAsync(MakeContext(), output);

        Assert.Equal("[Email]", output.Content.GetContent());
    }

    [Fact]
    public async Task ProcessAsync_RemovesPlaceholderAttribute()
    {
        var helper = new ProtectedTagHelper(DefaultOptions);
        var attrs  = new TagHelperAttributeList { { "placeholder", "[Hidden]" } };
        var output = MakeOutput("data", attrs);

        await helper.ProcessAsync(MakeContext(), output);

        Assert.False(output.Attributes.ContainsName("placeholder"));
    }

    [Fact]
    public async Task ProcessAsync_PreservesClassAttribute()
    {
        var helper = new ProtectedTagHelper(DefaultOptions);
        var attrs  = new TagHelperAttributeList { { "class", "protected-lnk" } };
        var output = MakeOutput("user@example.com", attrs);

        await helper.ProcessAsync(MakeContext(), output);

        Assert.True(output.Attributes.ContainsName("class"));
        Assert.Equal("protected-lnk", output.Attributes["class"].Value.ToString());
    }

    [Fact]
    public async Task ProcessAsync_DefaultPlaceholder_UsedFromOptions()
    {
        var opts   = Options.Create(new CrawlerProtectOptions { DefaultPlaceholder = "[Custom]" });
        var helper = new ProtectedTagHelper(opts);
        var output = MakeOutput("secret");

        await helper.ProcessAsync(MakeContext(), output);

        Assert.Equal("[Custom]", output.Content.GetContent());
    }

    [Fact]
    public async Task ProcessAsync_DataProtectedIsDecodable()
    {
        var helper = new ProtectedTagHelper(DefaultOptions);
        var email  = "user@example.com";
        var output = MakeOutput(email);

        await helper.ProcessAsync(MakeContext(), output);

        var attr = output.Attributes["data-protected"].Value.ToString()!;
        var sep  = attr.IndexOf('*');
        var key  = int.Parse(attr[..sep]);
        var hex  = attr[(sep + 1)..];

        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < hex.Length; i += 2)
            bytes[i / 2] = (byte)(Convert.ToInt32(hex.Substring(i, 2), 16) ^ key);

        Assert.Equal(email, Encoding.UTF8.GetString(bytes));
    }
}

public class CrawlerProtectJsGeneratorTests
{
    // ── Separator ─────────────────────────────────────────────────────────

    [Fact]
    public void Generate_DefaultOptions_UsesStarSeparator()
    {
        var js = CrawlerProtectJsGenerator.Generate(new CrawlerProtectOptions());

        Assert.Contains("indexOf(\"*\")", js);
    }

    [Fact]
    public void Generate_CustomSeparator_BakedIntoScript()
    {
        var opts = new CrawlerProtectOptions { Separator = '|' };
        var js   = CrawlerProtectJsGenerator.Generate(opts);

        Assert.Contains("indexOf(\"|\")", js);
        Assert.DoesNotContain("indexOf(\"*\")", js);
    }

    // ── Key position ─────────────────────────────────────────────────────

    [Fact]
    public void Generate_KeyBefore_UsesIndexOf()
    {
        var opts = new CrawlerProtectOptions { KeyPosition = KeyPosition.Before };
        var js   = CrawlerProtectJsGenerator.Generate(opts);

        Assert.Contains("parseInt(e.substring(0,si),10)", js); // key is before separator
    }

    [Fact]
    public void Generate_KeyAfter_UsesLastIndexOf()
    {
        var opts = new CrawlerProtectOptions { KeyPosition = KeyPosition.After };
        var js   = CrawlerProtectJsGenerator.Generate(opts);

        Assert.Contains("lastIndexOf(", js);
        Assert.Contains("parseInt(e.substring(si+1),10)", js); // key is after separator
    }

    // ── Payload encoding ─────────────────────────────────────────────────

    [Fact]
    public void Generate_HexEncoding_UsesParseInt16()
    {
        var opts = new CrawlerProtectOptions { PayloadEncoding = PayloadEncoding.Hex };
        var js   = CrawlerProtectJsGenerator.Generate(opts);

        Assert.Contains("parseInt(data.substring(i,i+2),16)", js);
        Assert.DoesNotContain("atob(", js);
    }

    [Fact]
    public void Generate_Base64Encoding_UsesAtob()
    {
        var opts = new CrawlerProtectOptions { PayloadEncoding = PayloadEncoding.Base64 };
        var js   = CrawlerProtectJsGenerator.Generate(opts);

        Assert.Contains("atob(data)", js);
        Assert.DoesNotContain("parseInt(data.substring(i,i+2),16)", js);
    }

    // ── Data attribute ────────────────────────────────────────────────────

    [Fact]
    public void Generate_CustomDataAttribute_BakedIntoScript()
    {
        var opts = new CrawlerProtectOptions { DataAttribute = "data-enc" };
        var js   = CrawlerProtectJsGenerator.Generate(opts);

        Assert.Contains("getAttribute(\"data-enc\")", js);
        Assert.Contains("removeAttribute(\"data-enc\")", js);
        Assert.DoesNotContain("data-protected", js);
    }

    [Fact]
    public void Generate_DefaultDataAttribute_IsDataProtected()
    {
        var js = CrawlerProtectJsGenerator.Generate(new CrawlerProtectOptions());

        Assert.Contains("getAttribute(\"data-protected\")", js);
    }

    // ── Structure ─────────────────────────────────────────────────────────

    [Fact]
    public void Generate_ScriptIsIife()
    {
        var js = CrawlerProtectJsGenerator.Generate(new CrawlerProtectOptions());

        Assert.StartsWith("(function(){", js);
        Assert.EndsWith("})();", js);
    }

    [Fact]
    public void Generate_ScriptAutoRunsBothParsers()
    {
        var js = CrawlerProtectJsGenerator.Generate(new CrawlerProtectOptions());

        Assert.Contains("_l(\"protected-lnk\")", js);
        Assert.Contains("_s(\"protected-str\")", js);
    }

    // ── Different configs produce different scripts ───────────────────────

    [Fact]
    public void Generate_DifferentConfigs_ProduceDifferentScripts()
    {
        var jsDefault = CrawlerProtectJsGenerator.Generate(new CrawlerProtectOptions());
        var jsCustom  = CrawlerProtectJsGenerator.Generate(new CrawlerProtectOptions
        {
            Separator       = '|',
            KeyPosition     = KeyPosition.After,
            PayloadEncoding = PayloadEncoding.Base64,
            DataAttribute   = "data-enc",
        });

        Assert.NotEqual(jsDefault, jsCustom);
    }
}
