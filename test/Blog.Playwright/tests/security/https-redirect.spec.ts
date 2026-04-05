import { test, expect } from '@playwright/test';

test.describe('HTTPS Redirect - L2-026', () => {
  test('HTTP request returns 301 redirect to HTTPS equivalent URL', async ({ request }) => {
    const response = await request.get('http://localhost:5000/', {
      maxRedirects: 0,
    });

    expect(response.status()).toBe(301);

    const location = response.headers()['location'];
    expect(location).toBeDefined();
    expect(location).toMatch(/^https:\/\//);
  });
});
