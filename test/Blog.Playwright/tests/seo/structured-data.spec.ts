import { test, expect } from '@playwright/test';

test.describe('L2-008: Structured Data (JSON-LD)', () => {
  const articleSlug = 'hello-world';
  const articleUrl = `/articles/${articleSlug}`;
  const listingUrl = '/';

  test.describe('Article detail page', () => {
    test('has <script type="application/ld+json"> with Schema.org Article', async ({ page }) => {
      await page.goto(articleUrl);

      const jsonLdScript = page.locator('script[type="application/ld+json"]');
      await expect(jsonLdScript.first()).toBeAttached();

      const jsonLdText = await jsonLdScript.first().textContent();
      expect(jsonLdText).toBeTruthy();

      const jsonLd = JSON.parse(jsonLdText!);
      expect(jsonLd['@type']).toBe('Article');
      expect(jsonLd['@context']).toMatch(/schema\.org/);
    });

    test('JSON-LD contains headline, datePublished, dateModified, author, description, image', async ({
      page,
    }) => {
      await page.goto(articleUrl);

      const jsonLdScript = page.locator('script[type="application/ld+json"]');
      const jsonLdText = await jsonLdScript.first().textContent();
      expect(jsonLdText).toBeTruthy();

      const jsonLd = JSON.parse(jsonLdText!);

      expect(jsonLd).toHaveProperty('headline');
      expect(jsonLd.headline).toBeTruthy();

      expect(jsonLd).toHaveProperty('datePublished');
      expect(new Date(jsonLd.datePublished).toString()).not.toBe('Invalid Date');

      expect(jsonLd).toHaveProperty('dateModified');
      expect(new Date(jsonLd.dateModified).toString()).not.toBe('Invalid Date');

      expect(jsonLd).toHaveProperty('author');
      expect(jsonLd.author).toBeTruthy();

      expect(jsonLd).toHaveProperty('description');
      expect(jsonLd.description).toBeTruthy();

      expect(jsonLd).toHaveProperty('image');
      expect(jsonLd.image).toBeTruthy();
    });

    test('JSON-LD is valid parseable JSON', async ({ page }) => {
      await page.goto(articleUrl);

      const jsonLdScripts = page.locator('script[type="application/ld+json"]');
      const count = await jsonLdScripts.count();
      expect(count).toBeGreaterThan(0);

      for (let i = 0; i < count; i++) {
        const text = await jsonLdScripts.nth(i).textContent();
        expect(text).toBeTruthy();
        expect(() => JSON.parse(text!)).not.toThrow();
      }
    });
  });

  test.describe('Listing page', () => {
    test('has JSON-LD with Schema.org Blog type', async ({ page }) => {
      await page.goto(listingUrl);

      const jsonLdScript = page.locator('script[type="application/ld+json"]');
      await expect(jsonLdScript.first()).toBeAttached();

      const jsonLdText = await jsonLdScript.first().textContent();
      expect(jsonLdText).toBeTruthy();

      const jsonLd = JSON.parse(jsonLdText!);
      expect(jsonLd['@type']).toBe('Blog');
      expect(jsonLd['@context']).toMatch(/schema\.org/);
    });
  });

  test.describe('Articles listing page', () => {
    test('has JSON-LD with Schema.org Blog type', async ({ page }) => {
      await page.goto('/articles');

      const jsonLdScript = page.locator('script[type="application/ld+json"]');
      await expect(jsonLdScript.first()).toBeAttached();

      const jsonLdText = await jsonLdScript.first().textContent();
      expect(jsonLdText).toBeTruthy();

      const jsonLd = JSON.parse(jsonLdText!);
      expect(jsonLd['@type']).toBe('Blog');
      expect(jsonLd['@context']).toMatch(/schema\.org/);
    });
  });
});
