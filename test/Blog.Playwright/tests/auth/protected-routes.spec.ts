import { test, expect } from '../../fixtures/base.fixture';
import { LoginPage } from '../../page-objects/back-office/login.page';
import { ArticleListPage } from '../../page-objects/back-office/article-list.page';

test.use({ storageState: { cookies: [], origins: [] } });

test.describe('Protected routes – unauthenticated access', () => {
  test('visiting /articles without auth redirects to /login', async ({ page }) => {
    await page.goto('/articles');

    await expect(page).toHaveURL(/\/login$/);
  });

  test('visiting /articles/new without auth redirects to /login', async ({ page }) => {
    await page.goto('/articles/new');

    await expect(page).toHaveURL(/\/login$/);
  });

  test('visiting /articles/{id}/edit without auth redirects to /login', async ({ page }) => {
    await page.goto('/articles/1/edit');

    await expect(page).toHaveURL(/\/login$/);
  });
});
