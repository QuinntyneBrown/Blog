import { test, expect } from '../../fixtures/base.fixture';
import { LoginPage } from '../../page-objects/back-office/login.page';
import { ArticleListPage } from '../../page-objects/back-office/article-list.page';

test.describe('Expired JWT session handling', () => {
  test('expired token on a protected page redirects to /login', async ({ page }) => {
    // Simulate an expired JWT by injecting a token that has already expired
    await page.context().addCookies([
      {
        name: 'token',
        value: 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwiZXhwIjoxNjAwMDAwMDAwfQ.invalid-expired-token',
        domain: 'localhost',
        path: '/',
      },
    ]);

    await page.goto('/admin/articles');

    await expect(page).toHaveURL(/\/login$/);
  });

  test('expired token on API request returns 401', async ({ page }) => {
    const response = await page.request.get('/api/articles', {
      headers: {
        Authorization:
          'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwiZXhwIjoxNjAwMDAwMDAwfQ.invalid-expired-token',
      },
    });

    expect(response.status()).toBe(401);
  });
});
