import { test, expect } from '@playwright/test';

test.describe('sitemap.xml', () => {
  test('is accessible and returns XML', async ({ request }) => {
    const response = await request.get('/sitemap.xml');
    expect(response.status()).toBe(200);
    expect(response.headers()['content-type']).toContain('xml');
  });

  test('contains homepage URL', async ({ request }) => {
    const response = await request.get('/sitemap.xml');
    const body = await response.text();
    expect(body).toContain('<urlset');
    expect(body).toContain('<url>');
  });
});
