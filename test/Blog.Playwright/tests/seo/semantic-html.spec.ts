import { test, expect } from '@playwright/test';

test.describe('L2-007: Semantic HTML', () => {
  const articleSlug = 'hello-world';
  const articleUrl = `/articles/${articleSlug}`;

  test('article content is wrapped in <article> element', async ({ page }) => {
    await page.goto(articleUrl);

    const article = page.locator('article');
    await expect(article).toBeVisible();
  });

  test('article title is the only <h1> on the page', async ({ page }) => {
    await page.goto(articleUrl);

    const h1Elements = page.locator('h1');
    const count = await h1Elements.count();
    expect(count).toBe(1);

    const h1Text = await h1Elements.first().textContent();
    expect(h1Text).toBeTruthy();
    expect(h1Text!.trim().length).toBeGreaterThan(0);
  });

  test('heading levels are sequential with no skipped levels', async ({ page }) => {
    await page.goto(articleUrl);

    const headingLevels = await page.evaluate(() => {
      const headings = document.querySelectorAll('h1, h2, h3, h4, h5, h6');
      return Array.from(headings).map((h) => parseInt(h.tagName.charAt(1), 10));
    });

    expect(headingLevels.length).toBeGreaterThan(0);
    expect(headingLevels[0]).toBe(1);

    for (let i = 1; i < headingLevels.length; i++) {
      const current = headingLevels[i];
      const previous = headingLevels[i - 1];
      // A heading can go deeper by at most 1 level, or go back up to any level
      expect(
        current <= previous + 1,
        `Heading level jumped from h${previous} to h${current} at position ${i}`,
      ).toBeTruthy();
    }
  });

  test('page has <main> element', async ({ page }) => {
    await page.goto(articleUrl);

    const main = page.locator('main');
    await expect(main).toBeVisible();
  });

  test('page has <nav> element', async ({ page }) => {
    await page.goto(articleUrl);

    const nav = page.locator('nav').first();
    await expect(nav).toBeVisible();
  });

  test('images within articles are wrapped in <figure> elements', async ({ page }) => {
    await page.goto(articleUrl);

    const articleImages = page.locator('article img');
    const imageCount = await articleImages.count();

    // Only check if there are images in the article
    expect(imageCount).toBeGreaterThan(0);

    for (let i = 0; i < imageCount; i++) {
      const img = articleImages.nth(i);
      const parentFigure = img.locator('xpath=ancestor::figure');
      await expect(
        parentFigure,
        `Image ${i + 1} should be wrapped in a <figure> element`,
      ).toBeAttached();
    }
  });
});
