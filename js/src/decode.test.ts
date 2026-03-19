import { beforeEach, describe, expect, it } from "vitest";
import { decodeStr, parse_protectedLnk, parse_protectedStr } from "./decode";

// ── helpers ──────────────────────────────────────────────────────────────────

/** Reproduce the C# EncodeString logic so tests are self-contained. */
function encode(str: string, key: number): string {
  let hex = "";
  for (const ch of str) {
    hex += (ch.charCodeAt(0) ^ key).toString(16).padStart(2, "0");
  }
  return `${key}*${hex}`;
}

// ── decodeStr ─────────────────────────────────────────────────────────────────

describe("decodeStr", () => {
  it("decodes a known payload (key 42, 'hello')", () => {
    // h=104^42=66=0x42  e=101^42=79=0x4f  l=108^42=70=0x46  l=0x46  o=111^42=69=0x45
    expect(decodeStr("42*424f464645")).toBe("hello");
  });

  it("is the inverse of the C# encoding (roundtrip)", () => {
    const cases: Array<[string, number]> = [
      ["test@example.com", 42],
      ["hello world", 1],
      ["sensitive data", 255],
      ["café", 100],
    ];
    for (const [text, key] of cases) {
      expect(decodeStr(encode(text, key))).toBe(text);
    }
  });

  it("returns empty string for empty payload", () => {
    expect(decodeStr("42*")).toBe("");
  });

  it("works at key boundary 1", () => {
    expect(decodeStr(encode("abc", 1))).toBe("abc");
  });

  it("works at key boundary 255", () => {
    expect(decodeStr(encode("abc", 255))).toBe("abc");
  });
});

// ── parse_protectedLnk ────────────────────────────────────────────────────────

describe("parse_protectedLnk", () => {
  beforeEach(() => {
    document.body.innerHTML = "";
  });

  it("decodes the email and sets a mailto href", () => {
    const email = "user@example.com";
    document.body.innerHTML = `<a class="pl" data-protected="${encode(email, 42)}">[Protected]</a>`;

    parse_protectedLnk("pl");

    const el = document.querySelector<HTMLAnchorElement>(".pl")!;
    expect(el.textContent).toBe(email);
    expect(el.href).toBe(`mailto:${email}`);
  });

  it("removes the data-protected attribute after decoding", () => {
    const email = "a@b.com";
    document.body.innerHTML = `<a class="pl" data-protected="${encode(email, 10)}">[Protected]</a>`;

    parse_protectedLnk("pl");

    expect(document.querySelector(".pl")!.getAttribute("data-protected")).toBeNull();
  });

  it("handles multiple elements", () => {
    const emails = ["first@example.com", "second@example.com"];
    document.body.innerHTML = emails
      .map((e) => `<a class="pl" data-protected="${encode(e, 5)}">[Protected]</a>`)
      .join("");

    parse_protectedLnk("pl");

    const elements = document.querySelectorAll<HTMLAnchorElement>(".pl");
    expect(elements[0].textContent).toBe(emails[0]);
    expect(elements[1].textContent).toBe(emails[1]);
  });

  it("ignores elements that have no data-protected attribute", () => {
    document.body.innerHTML = `<a class="pl">[Static]</a>`;

    // Should not throw
    expect(() => parse_protectedLnk("pl")).not.toThrow();
    expect(document.querySelector(".pl")!.textContent).toBe("[Static]");
  });

  it("preserves existing classes on the element", () => {
    const email = "x@y.com";
    document.body.innerHTML = `<a class="pl extra" data-protected="${encode(email, 7)}">[Protected]</a>`;

    parse_protectedLnk("pl");

    const el = document.querySelector(".pl")!;
    expect(el.classList.contains("extra")).toBe(true);
  });
});

// ── parse_protectedStr ────────────────────────────────────────────────────────

describe("parse_protectedStr", () => {
  beforeEach(() => {
    document.body.innerHTML = "";
  });

  it("replaces the anchor with a span containing the decoded text", () => {
    const text = "sensitive data";
    document.body.innerHTML = `<div><a class="ps" data-protected="${encode(text, 50)}">[Protected]</a></div>`;

    parse_protectedStr("ps");

    expect(document.querySelector("a")).toBeNull();
    const span = document.querySelector("span");
    expect(span).not.toBeNull();
    expect(span!.textContent).toBe(text);
  });

  it("copies all attributes except data-protected to the new span", () => {
    const text = "data";
    document.body.innerHTML = `<div><a class="ps extra" id="my-id" data-protected="${encode(text, 20)}">[P]</a></div>`;

    parse_protectedStr("ps");

    const span = document.querySelector("span")!;
    expect(span.classList.contains("ps")).toBe(true);
    expect(span.classList.contains("extra")).toBe(true);
    expect(span.id).toBe("my-id");
    expect(span.getAttribute("data-protected")).toBeNull();
  });

  it("handles multiple elements", () => {
    const items = ["foo", "bar"];
    document.body.innerHTML = items
      .map((t) => `<div><a class="ps" data-protected="${encode(t, 3)}">[P]</a></div>`)
      .join("");

    parse_protectedStr("ps");

    const spans = document.querySelectorAll("span");
    expect(spans).toHaveLength(2);
    expect(spans[0].textContent).toBe(items[0]);
    expect(spans[1].textContent).toBe(items[1]);
  });

  it("ignores elements with no data-protected attribute", () => {
    document.body.innerHTML = `<div><a class="ps">[Static]</a></div>`;

    expect(() => parse_protectedStr("ps")).not.toThrow();
    // Element is not replaced when there is nothing to decode
    expect(document.querySelector("a")).not.toBeNull();
  });
});
