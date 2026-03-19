using System.Text;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Xunit;

namespace CrawlerProtect.Tests;

public class EncodeStringTests
{
    [Fact]
    public void EncodeString_ReturnsFormatKeyStarHex()
    {
        var result = ProtectedTagHelper.EncodeString("hello", 42);

        Assert.Matches(@"^\d+\*[0-9a-f]+$", result);
    }

    [Fact]
    public void EncodeString_PrefixMatchesKey()
    {
        var result = ProtectedTagHelper.EncodeString("hello", 42);

        Assert.StartsWith("42*", result);
    }

    [Theory]
    [InlineData("test@example.com", 42)]
    [InlineData("hello world", 1)]
    [InlineData("sensitive data", 255)]
    [InlineData("café", 100)]
    public void EncodeString_IsSymmetricWithXor(string input, int key)
    {
        var encoded = ProtectedTagHelper.EncodeString(input, key);

        var sep = encoded.IndexOf('*');
        var hexStr = encoded[(sep + 1)..];

        var decoded = new StringBuilder();
        for (int i = 0; i < hexStr.Length; i += 2)
        {
            var charCode = Convert.ToInt32(hexStr.Substring(i, 2), 16) ^ key;
            decoded.Append((char)charCode);
        }

        Assert.Equal(input, decoded.ToString());
    }

    [Fact]
    public void EncodeString_EmptyInput_ReturnsKeyWithNoHex()
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

        // Same content, different keys → different hex payload
        var hex1 = result1[(result1.IndexOf('*') + 1)..];
        var hex2 = result2[(result2.IndexOf('*') + 1)..];
        Assert.NotEqual(hex1, hex2);
    }

    [Fact]
    public void EncodeString_HexPayloadLengthIsTwiceInputLength()
    {
        var input = "hello";
        var result = ProtectedTagHelper.EncodeString(input, 42);

        var hex = result[(result.IndexOf('*') + 1)..];
        Assert.Equal(input.Length * 2, hex.Length);
    }
}

public class ProtectedTagHelperProcessTests
{
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
        var helper = new ProtectedTagHelper();
        var output = MakeOutput("user@example.com");

        await helper.ProcessAsync(MakeContext(), output);

        Assert.Equal("a", output.TagName);
    }

    [Fact]
    public async Task ProcessAsync_SetsDataProtectedAttribute()
    {
        var helper = new ProtectedTagHelper();
        var output = MakeOutput("user@example.com");

        await helper.ProcessAsync(MakeContext(), output);

        Assert.True(output.Attributes.ContainsName("data-protected"));
        var attr = output.Attributes["data-protected"].Value.ToString()!;
        Assert.Matches(@"^\d+\*[0-9a-f]+$", attr);
    }

    [Fact]
    public async Task ProcessAsync_ContentIsPlaceholder()
    {
        var helper = new ProtectedTagHelper { Placeholder = "[Email]" };
        var output = MakeOutput("user@example.com");

        await helper.ProcessAsync(MakeContext(), output);

        Assert.Equal("[Email]", output.Content.GetContent());
    }

    [Fact]
    public async Task ProcessAsync_RemovesPlaceholderAttribute()
    {
        var helper = new ProtectedTagHelper();
        var attrs = new TagHelperAttributeList { { "placeholder", "[Hidden]" } };
        var output = MakeOutput("data", attrs);

        await helper.ProcessAsync(MakeContext(), output);

        Assert.False(output.Attributes.ContainsName("placeholder"));
    }

    [Fact]
    public async Task ProcessAsync_PreservesClassAttribute()
    {
        var helper = new ProtectedTagHelper();
        var attrs = new TagHelperAttributeList { { "class", "protected-lnk" } };
        var output = MakeOutput("user@example.com", attrs);

        await helper.ProcessAsync(MakeContext(), output);

        Assert.True(output.Attributes.ContainsName("class"));
        Assert.Equal("protected-lnk", output.Attributes["class"].Value.ToString());
    }

    [Fact]
    public async Task ProcessAsync_DataProtectedIsDecodable()
    {
        var helper = new ProtectedTagHelper();
        var email = "user@example.com";
        var output = MakeOutput(email);

        await helper.ProcessAsync(MakeContext(), output);

        var attr = output.Attributes["data-protected"].Value.ToString()!;
        var sep = attr.IndexOf('*');
        var key = int.Parse(attr[..sep]);
        var hex = attr[(sep + 1)..];

        var decoded = new StringBuilder();
        for (int i = 0; i < hex.Length; i += 2)
        {
            var charCode = Convert.ToInt32(hex.Substring(i, 2), 16) ^ key;
            decoded.Append((char)charCode);
        }

        Assert.Equal(email, decoded.ToString());
    }
}
