import { test, expect } from '../../fixtures/base.fixture';

test.describe('L2-015: Slug Normalization and Redirects', () => {
  test('mixed-case slug 301 redirects to lowercase', async ({ page }) => {
    const response = await page.goto('/articles/My-Article');

    // Verify the response chain included a 301 redirect
    expect(response).not.toBeNull();
    expect(response!.status()).toBe(200);

    // The final URL should be the lowercase version
    expect(page.url()).toMatch(/\/articles\/my-article$/);

    // Verify the redirect was a 301 (permanent) by checking the request chain
    const request = response!.request();
    const redirectedFrom = request.redirectedFrom();
    expect(redirectedFrom).not.toBeNull();
    expect(redirectedFrom!.url()).toMatch(/\/articles\/My-Article/);

    const redirectResponse = await redirectedFrom!.response();
    expect(redirectResponse).not.toBeNull();
    expect(redirectResponse!.status()).toBe(301);
  });

  test('numeric ID returns 404', async ({ page, notFoundPage }) => {
    await page.goto('/articles/123');

    const visible = await notFoundPage.isVisible();
    expect(visible).toBe(true);

    await expect(notFoundPage.heading).toBeVisible();
  });
});
