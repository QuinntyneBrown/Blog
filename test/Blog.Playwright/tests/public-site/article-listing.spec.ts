import { test, expect } from '@playwright/test';
import { PublicArticleListPage } from '../../page-objects/public/article-list.page';

test.describe('Public Article Listing', () => {
  test('homepage shows article grid or empty state', async ({ page }) => {
    const listPage = new PublicArticleListPage(page);
    await listPage.goto();

    const cardCount = await listPage.getCardCount();
    if (cardCount === 0) {
      await expect(listPage.emptyState).toBeVisible();
    } else {
      await expect(listPage.articleCards.first()).toBeVisible();
    }
  });

  test('nav is visible on listing page', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('.nav')).toBeVisible();
    await expect(page.locator('.nav-logo')).toBeVisible();
  });

  test('footer is visible on listing page', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('.footer')).toBeVisible();
  });
});
