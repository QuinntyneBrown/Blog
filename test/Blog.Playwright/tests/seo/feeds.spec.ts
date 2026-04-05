import { test, expect } from '@playwright/test';

test.describe('RSS and Atom Feeds', () => {
  test('RSS feed is accessible', async ({ request }) => {
    const response = await request.get('/feed/rss');
    expect(response.status()).toBe(200);
    const body = await response.text();
    expect(body).toContain('<rss');
  });

  test('Atom feed is accessible', async ({ request }) => {
    const response = await request.get('/feed/atom');
    expect(response.status()).toBe(200);
    const body = await response.text();
    expect(body).toContain('feed');
  });

  test('JSON feed is accessible', async ({ request }) => {
    const response = await request.get('/feed/json');
    expect(response.status()).toBe(200);
  });
});
