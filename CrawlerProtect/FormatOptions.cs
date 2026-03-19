namespace CrawlerProtect;

/// <summary>Position of the XOR key within the encoded value.</summary>
public enum KeyPosition
{
    /// <summary>Key precedes the payload: <c>key{sep}data</c> (e.g. <c>42*424f4645</c>).</summary>
    Before,

    /// <summary>Key follows the payload: <c>data{sep}key</c> (e.g. <c>424f4645*42</c>).</summary>
    After,
}

/// <summary>Encoding applied to the XOR-encrypted payload bytes.</summary>
public enum PayloadEncoding
{
    /// <summary>Lowercase hexadecimal pairs (e.g. <c>424f4645</c>).</summary>
    Hex,

    /// <summary>RFC 4648 base-64 (e.g. <c>Qk9GRQ==</c>).</summary>
    Base64,
}
