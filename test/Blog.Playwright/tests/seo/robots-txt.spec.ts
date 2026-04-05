import { test, expect } from '@playwright/test';

test.describe('L2-013: robots.txt', () => {
  test('GET /robots.txt returns text with User-agent: *', async ({ page }) => {
    const response = await page.request.get('/robots.txt');
    expect(response.ok()).toBeTruthy();

    const body = await response.text();
    expect(body).toContain('User-agent: *');
  });

  test('contains Allow: /', async ({ page }) => {
    const response = await page.request.get('/robots.txt');
    expect(response.ok()).toBeTruthy();

    const body = await response.text();
    expect(body).toContain('Allow: /');
  });

  test('contains Disallow: /api/', async ({ page }) => {
    const response = await page.request.get('/robots.txt');
    expect(response.ok()).toBeTruthy();

    const body = await response.text();
    expect(body).toContain('Disallow: /api/');
  });

  test('contains Sitemap directive pointing to /sitemap.xml', async ({ page }) => {
    const response = await page.request.get('/robots.txt');
    expect(response.ok()).toBeTruthy();

    const body = await response.text();
    expect(body).toMatch(/Sitemap:\s*.*\/sitemap\.xml/);
  });

  test('Content-Type is text/plain', async ({ page }) => {
    const response = await page.request.get('/robots.txt');
    expect(response.ok()).toBeTruthy();

    const contentType = response.headers()['content-type'];
    expect(contentType).toMatch(/text\/plain/);
  });
});
