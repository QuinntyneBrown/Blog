import { test, expect } from '@playwright/test';

test.describe('L2-016: llms.txt', () => {
  test('GET /llms.txt returns a text response', async ({ page }) => {
    const response = await page.request.get('/llms.txt');
    expect(response.ok()).toBeTruthy();

    const body = await response.text();
    expect(body.length).toBeGreaterThan(0);
  });

  test('contains site name and brief description', async ({ page }) => {
    const response = await page.request.get('/llms.txt');
    expect(response.ok()).toBeTruthy();

    const body = await response.text();

    // Should contain some form of site name/title
    // and a description/summary line
    const lines = body.split('\n').filter((line) => line.trim().length > 0);
    expect(lines.length).toBeGreaterThanOrEqual(2);

    // First non-empty line should be the site name or heading
    expect(lines[0].trim().length).toBeGreaterThan(0);
  });

  test('contains URL for /sitemap.xml', async ({ page }) => {
    const response = await page.request.get('/llms.txt');
    expect(response.ok()).toBeTruthy();

    const body = await response.text();
    expect(body).toContain('/sitemap.xml');
  });

  test('contains URL for /feed.xml', async ({ page }) => {
    const response = await page.request.get('/llms.txt');
    expect(response.ok()).toBeTruthy();

    const body = await response.text();
    expect(body).toContain('/feed.xml');
  });

  test('contains URL for /atom.xml', async ({ page }) => {
    const response = await page.request.get('/llms.txt');
    expect(response.ok()).toBeTruthy();

    const body = await response.text();
    expect(body).toContain('/atom.xml');
  });

  test('Content-Type is text/plain', async ({ page }) => {
    const response = await page.request.get('/llms.txt');
    expect(response.ok()).toBeTruthy();

    const contentType = response.headers()['content-type'];
    expect(contentType).toMatch(/text\/plain/);
  });
});
