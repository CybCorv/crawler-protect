using Microsoft.Extensions.Options;

namespace CrawlerProtect;

/// <summary>
/// Validates <see cref="CrawlerProtectOptions"/> at startup.
/// Registered automatically by <see cref="ServiceCollectionExtensions.AddCrawlerProtect"/>.
/// </summary>
internal sealed class CrawlerProtectOptionsValidator : IValidateOptions<CrawlerProtectOptions>
{
    public ValidateOptionsResult Validate(string? name, CrawlerProtectOptions options)
    {
        var errors = new List<string>();

        // DataAttribute must be a valid data-* attribute name.
        if (!options.DataAttribute.StartsWith("data-", StringComparison.Ordinal))
            errors.Add(
                $"DataAttribute must start with \"data-\" (got \"{options.DataAttribute}\"). " +
                "Example: \"data-protected\" or \"data-enc\".");

        // Separator must not be a character that can appear in the encoded payload,
        // otherwise the split is ambiguous.
        ValidateSeparator(options, errors);

        // ScriptPath must be a root-relative URL.
        if (!options.ScriptPath.StartsWith('/'))
            errors.Add(
                $"ScriptPath must start with '/' (got \"{options.ScriptPath}\"). " +
                "Example: \"/crawler-protect.js\".");

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }

    private static void ValidateSeparator(CrawlerProtectOptions options, List<string> errors)
    {
        var c = options.Separator;

        if (options.PayloadEncoding == PayloadEncoding.Hex)
        {
            // Hex payload: [0-9a-fA-F] — separator must not be a hex digit.
            var lo = char.ToLowerInvariant(c);
            if ((lo >= '0' && lo <= '9') || (lo >= 'a' && lo <= 'f'))
                errors.Add(
                    $"Separator '{c}' is a hexadecimal digit and would be ambiguous inside a " +
                    "Hex-encoded payload. Choose a non-hex character (e.g. '*', '|', '!').");
        }
        else // Base64
        {
            // Base64 alphabet: [A-Za-z0-9+/=]
            if (char.IsLetterOrDigit(c) || c == '+' || c == '/' || c == '=')
                errors.Add(
                    $"Separator '{c}' can appear in a Base64-encoded payload and would be " +
                    "ambiguous. Choose a character outside [A-Za-z0-9+/=] (e.g. '*', '|', '!').");
        }
    }
}
