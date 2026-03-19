/**
 * Decodes a string that was XOR-encoded by the C# ProtectedTagHelper.
 *
 * @param encrypted - Encoded value in the format `key*hexstring`
 *   (e.g. `"42*424f464645"`)
 * @returns The original plaintext string.
 */
export function decodeStr(encrypted: string): string {
  const separatorIndex = encrypted.indexOf("*");
  const key = parseInt(encrypted.substring(0, separatorIndex), 10);
  const encryptedData = encrypted.substring(separatorIndex + 1);

  let result = "";
  for (let i = 0; i < encryptedData.length; i += 2) {
    const hexChar = encryptedData.substring(i, i + 2);
    result += String.fromCharCode(parseInt(hexChar, 16) ^ key);
  }
  return result;
}

/**
 * Decodes all elements with class `clname` as protected email links.
 * Each element must have a `data-protected` attribute containing the encoded email.
 * After decoding, the element's text and `href` (as `mailto:`) are updated.
 *
 * @param clname - CSS class name to target (e.g. `"protected-lnk"`)
 */
export function parse_protectedLnk(clname: string): void {
  const elements = Array.from(document.getElementsByClassName(clname));
  for (const el of elements) {
    const encoded = (el as HTMLElement).dataset["protected"];
    if (!encoded) continue;
    const decoded = decodeStr(encoded);
    el.textContent = decoded;
    el.removeAttribute("data-protected");
    (el as HTMLAnchorElement).href = "mailto:" + decoded;
  }
}

/**
 * Decodes all elements with class `clname` as protected text spans.
 * Each element must have a `data-protected` attribute containing the encoded text.
 * After decoding, the `<a>` element is replaced by a `<span>` with the plaintext.
 *
 * @param clname - CSS class name to target (e.g. `"protected-str"`)
 */
export function parse_protectedStr(clname: string): void {
  const elements = Array.from(document.getElementsByClassName(clname));
  for (const el of elements) {
    const encoded = (el as HTMLElement).dataset["protected"];
    if (!encoded) continue;
    const span = document.createElement("span");
    span.textContent = decodeStr(encoded);
    Array.from(el.attributes).forEach((attr) => {
      if (attr.nodeName !== "data-protected") {
        span.setAttribute(attr.nodeName, attr.nodeValue!);
      }
    });
    el.parentElement?.replaceChild(span, el);
  }
}

// Auto-run when included as a plain <script> tag
parse_protectedLnk("protected-lnk");
parse_protectedStr("protected-str");
