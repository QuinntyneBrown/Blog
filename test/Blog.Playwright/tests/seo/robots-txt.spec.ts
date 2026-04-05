import { test, expect } from '@playwright/test';

test.describe('robots.txt', () => {
  test('is accessible', async ({ request }) => {
    const response = await request.get('/robots.txt');
    expect(response.status()).toBe(200);
  });

  test('disallows /admin/ and /api/', async ({ request }) => {
    const response = await request.get('/robots.txt');
    const body = await response.text();
    expect(body).toContain('Disallow: /admin/');
    expect(body).toContain('Disallow: /api/');
  });

  test('includes Sitemap directive', async ({ request }) => {
    const response = await request.get('/robots.txt');
    const body = await response.text();
    expect(body).toContain('Sitemap:');
  });
});
