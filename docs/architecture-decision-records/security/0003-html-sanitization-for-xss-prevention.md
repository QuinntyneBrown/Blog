# ADR-0003: HTML Sanitization with HtmlSanitizer for XSS Prevention

**Date:** 2026-04-04
**Category:** security
**Status:** Accepted
**Deciders:** Architecture Team

## Context

Article body content is submitted as HTML by back-office users and rendered directly in the public site. This creates a stored XSS attack vector: if a malicious user injects `<script>` tags, event handlers (`onclick`, `onerror`), or other executable content into an article body, it would execute in every reader's browser when the article is rendered.

The platform must allow a subset of safe HTML tags for rich content formatting (headings, paragraphs, links, images, lists, code blocks) while stripping all dangerous elements. Sanitization must happen at write time so that stored content is always safe (L2-025).

## Decision

We will sanitize article body HTML at **write time** (create and update) using the **HtmlSanitizer** NuGet package (Ganss.Xss) with a configured allow-list of safe tags and attributes.

**Allowed tags:** `<p>`, `<h1>`-`<h6>`, `<a>`, `<img>`, `<ul>`, `<ol>`, `<li>`, `<strong>`, `<em>`, `<code>`, `<pre>`, `<blockquote>`, `<figure>`, `<figcaption>`.

**Stripped elements:** `<script>`, `<iframe>`, `<object>`, `<embed>`, `<form>`, `<input>`, `<style>`, and all event handler attributes (`onclick`, `onerror`, `onload`, etc.).

## Options Considered

### Option 1: Allow-List Sanitization at Write Time (HtmlSanitizer / Ganss.Xss)
- **Pros:** Content is sanitized once at persist time — stored data is always safe, allow-list approach is more secure than deny-list (only explicitly permitted tags survive), HtmlSanitizer is a well-maintained .NET library with configurable allow-lists, output is safe HTML that renders correctly without additional encoding.
- **Cons:** Sanitization at write time means existing unsafe content must be migrated, the allow-list must be maintained as content needs evolve (e.g., adding `<table>` support later).

### Option 2: Output Encoding at Render Time
- **Pros:** No write-time processing, content stored exactly as submitted.
- **Cons:** Must be applied on every render (easy to miss a rendering path), HTML encoding strips all formatting (content becomes plain text), does not support rich HTML content.

### Option 3: Markdown-Only Input (No HTML)
- **Pros:** Markdown is inherently safer than HTML, well-defined rendering rules.
- **Cons:** Limits content expressiveness, Markdown-to-HTML conversion still requires sanitization of the output, some authors prefer HTML editors.

### Option 4: Deny-List Approach (Strip Known Bad Tags)
- **Pros:** Less restrictive, preserves more of the original content.
- **Cons:** Fundamentally insecure — new attack vectors bypass the deny-list, impossible to enumerate all dangerous patterns (mutation XSS, encoding tricks), security researchers consistently find bypasses.

## Consequences

### Positive
- Stored content is guaranteed safe — no XSS payload can survive sanitization.
- Allow-list approach is resilient to novel attack vectors — unknown tags are stripped by default.
- Sanitization happens once at write time, not on every render (better performance, no missed rendering paths).
- The HtmlSanitizer library handles edge cases (nested tags, encoding tricks, mutation XSS) that manual sanitization would miss.

### Negative
- Authors cannot use arbitrary HTML — only the allow-listed tags are preserved.
- If the allow-list needs expansion (e.g., `<table>` for data content), a code change and review is required.
- Content re-saved after changing the allow-list may differ from the original.

### Risks
- The HtmlSanitizer library must be kept up to date to address newly discovered bypass techniques. Pin to a reviewed version and update regularly.

## Implementation Notes

- Sanitization is applied in the `ArticleService` during create and update operations, before persistence.
- `HtmlSanitizer` configured with `AllowedTags`, `AllowedAttributes` (e.g., `href`, `src`, `alt`, `class`), and `AllowedSchemes` (e.g., `https`, `mailto`).
- The sanitizer strips `javascript:` and `data:` URI schemes from `href` and `src` attributes.
- Unit tests verify that `<script>alert('xss')</script>` is stripped, that safe tags survive, and that event handler attributes are removed.

## References

- [HtmlSanitizer (Ganss.Xss)](https://github.com/mganss/HtmlSanitizer)
- [OWASP XSS Prevention Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Cross_Site_Scripting_Prevention_Cheat_Sheet.html)
- L2-025: Input Validation and Sanitization
- Feature 08: Security Hardening — HtmlSanitizer (Section 3.7)
