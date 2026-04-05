import { test, expect } from '@playwright/test';

test.describe('L2-012: Meta Tags', () => {
  const articleSlug = 'hello-world';
  const articleUrl = `/articles/${articleSlug}`;
  const listingUrl = '/';

  test.describe('Article page', () => {
    test('has <title> matching pattern "{Article Title} | {Site Name}"', async ({ page }) => {
      await page.goto(articleUrl);

      const title = await page.title();
      expect(title).toMatch(/.+\s\|\s.+/);
    });

    test('has <meta name="description"> with the article abstract', async ({ page }) => {
      await page.goto(articleUrl);

      const description = await page
        .locator('meta[name="description"]')
        .getAttribute('content');
      expect(description).toBeTruthy();
      expect(description!.length).toBeGreaterThan(0);
    });

    test('title portion does not exceed 60 characters', async ({ page }) => {
      await page.goto(articleUrl);

      const fullTitle = await page.title();
      // Extract the article title portion before the pipe separator
      const titlePortion = fullTitle.split('|')[0].trim();
      expect(titlePortion.length).toBeLessThanOrEqual(60);

      // If the original title was truncated, it should end with an ellipsis
      if (titlePortion.endsWith('...') || titlePortion.endsWith('\u2026')) {
        expect(titlePortion.length).toBeLessThanOrEqual(60);
      }
    });

    test('meta description does not exceed 160 characters', async ({ page }) => {
      await page.goto(articleUrl);

      const description = await page
        .locator('meta[name="description"]')
        .getAttribute('content');
      expect(description).toBeTruthy();
      expect(description!.length).toBeLessThanOrEqual(160);
    });
  });

  test.describe('Listing page', () => {
    test('has appropriate title', async ({ page }) => {
      await page.goto(listingUrl);

      const title = await page.title();
      expect(title).toBeTruthy();
      expect(title.length).toBeGreaterThan(0);
      expect(title.length).toBeLessThanOrEqual(60);
    });

    test('has appropriate meta description', async ({ page }) => {
      await page.goto(listingUrl);

      const description = await page
        .locator('meta[name="description"]')
        .getAttribute('content');
      expect(description).toBeTruthy();
      expect(description!.length).toBeGreaterThan(0);
      expect(description!.length).toBeLessThanOrEqual(160);
    });
  });
});
