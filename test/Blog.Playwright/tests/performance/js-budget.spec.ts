import { test, expect } from '@playwright/test';

test.describe('JavaScript Budget - L2-017', () => {
  test('total JS on public site is under 50KB gzipped', async ({ page }) => {
    let totalJsBytes = 0;

    page.on('response', async (response) => {
      const url = response.url();
      const contentType = response.headers()['content-type'] || '';

      if (url.endsWith('.js') || contentType.includes('javascript')) {
        const contentLength = response.headers()['content-length'];
        if (contentLength) {
          totalJsBytes += Number(contentLength);
        } else {
          const body = await response.body().catch(() => Buffer.alloc(0));
          totalJsBytes += body.length;
        }
      }
    });

    await page.goto('/');
    await page.waitForLoadState('networkidle');

    const fiftyKB = 50 * 1024;
    expect(totalJsBytes).toBeLessThanOrEqual(fiftyKB);
  });

  test('full article content is visible in initial HTML (SSR, not JS-rendered)', async ({ browser }) => {
    const context = await browser.newContext({ javaScriptEnabled: false });
    const page = await context.newPage();
    await page.goto('/articles/getting-started-with-aspnet-core');

    const articleBody = page.locator('article .article-body, .article-body');
    await expect(articleBody).toBeVisible();

    const textContent = await articleBody.textContent();
    expect(textContent!.length).toBeGreaterThan(100);

    await context.close();
  });
});
