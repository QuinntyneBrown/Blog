import { test, expect } from '@playwright/test';

test.describe('Protected Routes', () => {
  test('unauthenticated access to articles redirects to login', async ({ page }) => {
    await page.goto('/admin/articles');
    await expect(page).toHaveURL(/\/admin\/login/);
  });

  test('unauthenticated access to editor redirects to login', async ({ page }) => {
    await page.goto('/admin/articles/create');
    await expect(page).toHaveURL(/\/admin\/login/);
  });
});
