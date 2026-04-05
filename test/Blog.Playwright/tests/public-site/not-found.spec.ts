import { test, expect } from '@playwright/test';

test.describe('404 Page', () => {
  test('shows 404 for non-existent article slug', async ({ page }) => {
    await page.goto('/articles/this-article-does-not-exist-xyz');
    await expect(page.locator('.not-found-number, .not-found-title')).toBeVisible();
  });

  test('404 page has back to articles link', async ({ page }) => {
    await page.goto('/articles/this-slug-does-not-exist-abc');
    const backLink = page.locator('a[href="/articles"]');
    await expect(backLink).toBeVisible();
  });
});
