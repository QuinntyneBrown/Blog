import { test, expect } from '@playwright/test';

test.describe('L2-014: RSS and Atom Feeds', () => {
  test.describe('RSS Feed', () => {
    test('GET /feed.xml returns valid RSS 2.0 XML with <rss> root element', async ({ page }) => {
      const response = await page.request.get('/feed.xml');
      expect(response.ok()).toBeTruthy();

      const body = await response.text();
      expect(body).toContain('<?xml');
      expect(body).toContain('<rss');
      expect(body).toMatch(/version=["']2\.0["']/);
      expect(body).toContain('</rss>');
    });

    test('RSS feed entries contain title, link, pubDate, description', async ({ page }) => {
      const response = await page.request.get('/feed.xml');
      expect(response.ok()).toBeTruthy();

      const body = await response.text();

      // Extract <item> blocks
      const items = body.match(/<item>[\s\S]*?<\/item>/g);
      expect(items).toBeTruthy();
      expect(items!.length).toBeGreaterThan(0);

      for (const item of items!) {
        expect(item, 'RSS item should have <title>').toContain('<title>');
        expect(item, 'RSS item should have <link>').toContain('<link>');
        expect(item, 'RSS item should have <pubDate>').toContain('<pubDate>');
        expect(item, 'RSS item should have <description>').toContain('<description>');
      }
    });

    test('newly published articles appear in the RSS feed', async ({ page }) => {
      const response = await page.request.get('/feed.xml');
      expect(response.ok()).toBeTruthy();

      const body = await response.text();

      // At least one article entry should be present
      const items = body.match(/<item>[\s\S]*?<\/item>/g);
      expect(items).toBeTruthy();
      expect(items!.length).toBeGreaterThan(0);

      // The known published article should be in the feed
      expect(body).toMatch(/hello-world|Hello World/);
    });
  });

  test.describe('Atom Feed', () => {
    test('GET /atom.xml returns valid Atom XML with <feed> root element', async ({ page }) => {
      const response = await page.request.get('/atom.xml');
      expect(response.ok()).toBeTruthy();

      const body = await response.text();
      expect(body).toContain('<?xml');
      expect(body).toContain('<feed');
      expect(body).toMatch(/xmlns=["']http:\/\/www\.w3\.org\/2005\/Atom["']/);
      expect(body).toContain('</feed>');
    });

    test('Atom feed entries contain title, link, updated, summary', async ({ page }) => {
      const response = await page.request.get('/atom.xml');
      expect(response.ok()).toBeTruthy();

      const body = await response.text();

      // Extract <entry> blocks
      const entries = body.match(/<entry>[\s\S]*?<\/entry>/g);
      expect(entries).toBeTruthy();
      expect(entries!.length).toBeGreaterThan(0);

      for (const entry of entries!) {
        expect(entry, 'Atom entry should have <title>').toContain('<title>');
        expect(entry, 'Atom entry should have <link').toContain('<link');
        expect(entry, 'Atom entry should have <updated>').toContain('<updated>');
        expect(entry, 'Atom entry should have <summary>').toContain('<summary>');
      }
    });
  });
});
