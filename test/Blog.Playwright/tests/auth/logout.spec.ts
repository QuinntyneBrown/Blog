import { test, expect } from '../../fixtures/base.fixture';
import { TEST_ADMIN } from '../../fixtures/test-data';
import { LoginPage } from '../../page-objects/back-office/login.page';
import { ArticleListPage } from '../../page-objects/back-office/article-list.page';

test.describe('Logout flow', () => {
  test.beforeEach(async ({ loginPage, page }) => {
    await loginPage.goto();
    await loginPage.login(TEST_ADMIN.email, TEST_ADMIN.password);
    await expect(page).toHaveURL(/\/articles$/);
  });

  test('clicking logout clears session and redirects to /login', async ({ page }) => {
    const logoutButton = page.getByRole('button', { name: /log\s?out|sign\s?out/i });
    await logoutButton.click();

    await expect(page).toHaveURL(/\/login$/);

    const cookies = await page.context().cookies();
    const tokenCookie = cookies.find(c => c.name === 'token' || c.name === 'jwt');
    expect(tokenCookie).toBeUndefined();
  });

  test('after logout, visiting /articles redirects back to /login', async ({ page }) => {
    const logoutButton = page.getByRole('button', { name: /log\s?out|sign\s?out/i });
    await logoutButton.click();

    await expect(page).toHaveURL(/\/login$/);

    await page.goto('/admin/articles');
    await expect(page).toHaveURL(/\/login$/);
  });
});
