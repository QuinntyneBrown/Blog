import { test, expect } from '@playwright/test';

test.describe('Cache Headers - L2-019', () => {
  test('static CSS/JS assets have Cache-Control with max-age=31536000 and immutable', async ({ page }) => {
    const assetResponses: { url: string; cacheControl: string | undefined }[] = [];

    page.on('response', (response) => {
      const url = response.url();
      if (url.match(/\.(css|js)(\?.*)?$/)) {
        assetResponses.push({
          url,
          cacheControl: response.headers()['cache-control'],
        });
      }
    });

    await page.goto('/');

    expect(assetResponses.length).toBeGreaterThan(0);

    for (const asset of assetResponses) {
      expect(asset.cacheControl).toBeDefined();
      expect(asset.cacheControl).toContain('max-age=31536000');
      expect(asset.cacheControl).toContain('immutable');
    }
  });

  test('HTML pages have Cache-Control with short max-age and stale-while-revalidate', async ({ page }) => {
    const response = await page.goto('/');

    const cacheControl = response!.headers()['cache-control'];
    expect(cacheControl).toBeDefined();

    const maxAgeMatch = cacheControl.match(/max-age=(\d+)/);
    expect(maxAgeMatch).not.toBeNull();
    expect(Number(maxAgeMatch![1])).toBeLessThanOrEqual(3600);

    expect(cacheControl).toContain('stale-while-revalidate');
  });

  test('HTML responses include ETag header', async ({ page }) => {
    const response = await page.goto('/');

    const etag = response!.headers()['etag'];
    expect(etag).toBeDefined();
  });
});
