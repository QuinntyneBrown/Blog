import { test, expect } from '@playwright/test';

test.describe('L2-009: Open Graph Tags', () => {
  const articleSlug = 'hello-world';
  const articleUrl = `/articles/${articleSlug}`;
  const listingUrl = '/';

  test.describe('Article page', () => {
    const requiredOgTags = [
      'og:title',
      'og:description',
      'og:image',
      'og:url',
      'og:type',
      'og:site_name',
    ];

    for (const tag of requiredOgTags) {
      test(`has ${tag} meta tag`, async ({ page }) => {
        await page.goto(articleUrl);

        const content = await page
          .locator(`meta[property="${tag}"]`)
          .getAttribute('content');
        expect(content).toBeTruthy();
      });
    }

    test('all og: tags have non-empty values', async ({ page }) => {
      await page.goto(articleUrl);

      const ogTags = page.locator('meta[property^="og:"]');
      const count = await ogTags.count();
      expect(count).toBeGreaterThan(0);

      for (let i = 0; i < count; i++) {
        const content = await ogTags.nth(i).getAttribute('content');
        const property = await ogTags.nth(i).getAttribute('property');
        expect(content, `${property} should have a non-empty value`).toBeTruthy();
      }
    });

    test('og:type is "article" on article detail page', async ({ page }) => {
      await page.goto(articleUrl);

      const ogType = await page
        .locator('meta[property="og:type"]')
        .getAttribute('content');
      expect(ogType).toBe('article');
    });

    test('og:image is an absolute URL when article has a featured image', async ({ page }) => {
      await page.goto(articleUrl);

      const ogImage = await page
        .locator('meta[property="og:image"]')
        .getAttribute('content');
      expect(ogImage).toBeTruthy();
      expect(ogImage).toMatch(/^https?:\/\//);
    });
  });

  test.describe('Listing page', () => {
    test('og:type is "website"', async ({ page }) => {
      await page.goto(listingUrl);

      const ogType = await page
        .locator('meta[property="og:type"]')
        .getAttribute('content');
      expect(ogType).toBe('website');
    });
  });
});
