# crawler-protect

A TagHelper to hide sensitive contents (like email addresses) from malicious bots using XOR obfuscation.

Inspired by [Cloudflare email address obfuscation](https://developers.cloudflare.com/support/more-dashboard-apps/cloudflare-scrape-shield/what-is-email-address-obfuscation/).

## Repository structure

```
CrawlerProtect/            ← NuGet package (C# TagHelper)
CrawlerProtect.Tests/      ← xUnit tests for the TagHelper
CrawlerProtect.sln
js/
  src/
    decode.ts              ← TypeScript client-side decoder
    decode.test.ts         ← Vitest tests
  package.json
  tsconfig.json
  vitest.config.ts
```

## How it works

The Razor `<protected>` tag is rendered server-side as an `<a>` with a `data-protected` attribute holding the XOR-encoded content. The companion `decode.js` (compiled from `decode.ts`) restores the original value in the browser.

```
Razor → C# TagHelper → encoded HTML → browser + decode.js → plaintext
```

## Example

**Razor:**
```html
<p><protected class="protected-str">My sensitive data</protected></p>
<p><protected class="protected-lnk">test@example.com</protected></p>
```

**Rendered HTML (sent to client):**
```html
<p><a class="protected-str" data-protected="226*af9bc2...">[Protected]</a></p>
<p><a class="protected-lnk" data-protected="28*68796f...">[Protected]</a></p>
```

**After `decode.js` runs:**
```html
<p><span class="protected-str">My sensitive data</span></p>
<p><a class="protected-lnk" href="mailto:test@example.com">test@example.com</a></p>
```

## Tag attributes

| Attribute     | Required | Default       | Description                                  |
|---------------|----------|---------------|----------------------------------------------|
| `class`       | Yes      | —             | `protected-lnk` (email link) or `protected-str` (plain text) |
| `placeholder` | No       | `[Protected]` | Text shown before JS decodes the content     |
| `href`        | No       | `#`           | Link target before decoding                  |

## Setup

### 1. C# — add the NuGet package

```bash
dotnet add package CrawlerProtect
```

Register the TagHelper in `_ViewImports.cshtml`:

```cshtml
@addTagHelper CrawlerProtect.ProtectedTagHelper, CrawlerProtect
```

### 2. JavaScript — include the decoder

Compile from source:

```bash
cd js && npm install && npm run build
```

Then include the generated `js/dist/decode.js` in your layout:

```html
<script src="~/js/decode.js"></script>
```

### 3. Run the tests

**C#:**
```bash
dotnet test CrawlerProtect.sln
```

**JavaScript:**
```bash
cd js && npm test
```

## Disclaimer

This is **obfuscation, not encryption**. It prevents automated crawlers from harvesting data, but is not a security boundary against a determined attacker.
