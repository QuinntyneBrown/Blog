import { test, expect } from '@playwright/test';

test.describe('L2-011: Sitemap', () => {
  test('GET /sitemap.xml returns valid XML', async ({ page }) => {
    const response = await page.request.get('/sitemap.xml');
    expect(response.ok()).toBeTruthy();

    const body = await response.text();
    expect(body).toContain('<?xml');
    expect(body).toContain('<urlset');
    expect(body).toContain('</urlset>');
  });

  test('Content-Type is application/xml', async ({ page }) => {
    const response = await page.request.get('/sitemap.xml');
    expect(response.ok()).toBeTruthy();

    const contentType = response.headers()['content-type'];
    expect(contentType).toMatch(/application\/xml/);
  });

  test('sitemap contains <url> entries for published articles', async ({ page }) => {
    const response = await page.request.get('/sitemap.xml');
    expect(response.ok()).toBeTruthy();

    const body = await response.text();
    expect(body).toContain('<url>');
    expect(body).toContain('</url>');

    // Should contain at least one article URL
    expect(body).toMatch(/<loc>.*\/articles\/.*<\/loc>/);
  });

  test('each <url> entry has <loc>, <lastmod>, <changefreq>, <priority>', async ({ page }) => {
    const response = await page.request.get('/sitemap.xml');
    expect(response.ok()).toBeTruthy();

    const body = await response.text();

    // Extract all <url> blocks
    const urlBlocks = body.match(/<url>[\s\S]*?<\/url>/g);
    expect(urlBlocks).toBeTruthy();
    expect(urlBlocks!.length).toBeGreaterThan(0);

    for (const block of urlBlocks!) {
      expect(block, 'URL entry should have <loc>').toContain('<loc>');
      expect(block, 'URL entry should have <lastmod>').toContain('<lastmod>');
      expect(block, 'URL entry should have <changefreq>').toContain('<changefreq>');
      expect(block, 'URL entry should have <priority>').toContain('<priority>');
    }
  });

  test('unpublished articles are not in the sitemap', async ({ page }) => {
    const response = await page.request.get('/sitemap.xml');
    expect(response.ok()).toBeTruthy();

    const body = await response.text();

    // Unpublished/draft articles should not appear
    // Check that known draft slugs are not present
    expect(body).not.toContain('/articles/draft-');
    expect(body).not.toContain('/articles/unpublished-');
  });
});
