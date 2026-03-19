/**
 * Configuration matching CrawlerProtectOptions format properties.
 * All fields are optional — omitted fields use the C# defaults.
 */
export interface DecoderConfig {
  /** Separator character between key and payload. Default: `"*"` */
  separator?: string;
  /** `true` = key before payload (`key*data`), `false` = key after (`data*key`). Default: `true` */
  keyBefore?: boolean;
  /** `true` = base-64 payload, `false` = hex payload. Default: `false` */
  base64?: boolean;
  /** HTML attribute carrying the encoded value. Default: `"data-protected"` */
  dataAttribute?: string;
}

const DEFAULTS: Required<DecoderConfig> = {
  separator:     "*",
  keyBefore:     true,
  base64:        false,
  dataAttribute: "data-protected",
};

/**
 * Decodes a string that was XOR-encoded by the C# ProtectedTagHelper.
 *
 * @param encrypted - Encoded value produced by `EncodeString` (e.g. `"42*424f4645"`)
 * @param config    - Optional format config; must match the server-side CrawlerProtectOptions.
 * @returns The original plaintext string.
 */
export function decodeStr(encrypted: string, config?: DecoderConfig): string {
  const sep       = config?.separator     ?? DEFAULTS.separator;
  const keyBefore = config?.keyBefore     ?? DEFAULTS.keyBefore;
  const base64    = config?.base64        ?? DEFAULTS.base64;

  const si = keyBefore
    ? encrypted.indexOf(sep)
    : encrypted.lastIndexOf(sep);

  let key: number;
  let data: string;

  if (keyBefore) {
    key  = parseInt(encrypted.substring(0, si), 10);
    data = encrypted.substring(si + 1);
  } else {
    data = encrypted.substring(0, si);
    key  = parseInt(encrypted.substring(si + 1), 10);
  }

  let bytes: Uint8Array;
  if (base64) {
    const bin = atob(data);
    bytes = new Uint8Array(bin.length);
    for (let i = 0; i < bin.length; i++) bytes[i] = bin.charCodeAt(i) ^ key;
  } else {
    bytes = new Uint8Array(data.length / 2);
    for (let i = 0; i < data.length; i += 2)
      bytes[i / 2] = parseInt(data.substring(i, i + 2), 16) ^ key;
  }

  return new TextDecoder().decode(bytes);
}

/**
 * Decodes all elements with class `clname` as protected email links.
 * Each element must carry the encoded email in the configured data attribute.
 * After decoding, the element's text and `href` (as `mailto:`) are updated.
 *
 * @param clname - CSS class name to target (e.g. `"protected-lnk"`)
 * @param config - Optional format config; must match the server-side CrawlerProtectOptions.
 */
export function parse_protectedLnk(clname: string, config?: DecoderConfig): void {
  const attr     = config?.dataAttribute ?? DEFAULTS.dataAttribute;
  const elements = Array.from(document.getElementsByClassName(clname));
  for (const el of elements) {
    const encoded = (el as HTMLElement).getAttribute(attr);
    if (!encoded) continue;
    const decoded = decodeStr(encoded, config);
    el.textContent = decoded;
    el.removeAttribute(attr);
    (el as HTMLAnchorElement).href = "mailto:" + decoded;
  }
}

/**
 * Decodes all elements with class `clname` as protected text spans.
 * Each element must carry the encoded text in the configured data attribute.
 * After decoding, the `<a>` element is replaced by a `<span>` with the plaintext.
 *
 * @param clname - CSS class name to target (e.g. `"protected-str"`)
 * @param config - Optional format config; must match the server-side CrawlerProtectOptions.
 */
export function parse_protectedStr(clname: string, config?: DecoderConfig): void {
  const attr     = config?.dataAttribute ?? DEFAULTS.dataAttribute;
  const elements = Array.from(document.getElementsByClassName(clname));
  for (const el of elements) {
    const encoded = (el as HTMLElement).getAttribute(attr);
    if (!encoded) continue;
    const span = document.createElement("span");
    span.textContent = decodeStr(encoded, config);
    Array.from(el.attributes).forEach((a) => {
      if (a.nodeName !== attr) {
        span.setAttribute(a.nodeName, a.nodeValue!);
      }
    });
    el.parentElement?.replaceChild(span, el);
  }
}

// Auto-run with default config when included as a plain <script> tag.
// If you use non-default CrawlerProtectOptions, call these manually:
//   parse_protectedLnk("protected-lnk", { separator: "|", keyBefore: false });
parse_protectedLnk("protected-lnk");
parse_protectedStr("protected-str");
