import { test, expect } from '@playwright/test';

test.describe('Compression - L2-041', () => {
  test('request with Accept-Encoding: br, gzip returns Content-Encoding: br', async ({ request }) => {
    const response = await request.get('/', {
      headers: {
        'Accept-Encoding': 'br, gzip',
      },
    });

    const contentEncoding = response.headers()['content-encoding'];
    expect(contentEncoding).toBe('br');
  });

  test('request with Accept-Encoding: gzip returns Content-Encoding: gzip', async ({ request }) => {
    const response = await request.get('/', {
      headers: {
        'Accept-Encoding': 'gzip',
      },
    });

    const contentEncoding = response.headers()['content-encoding'];
    expect(contentEncoding).toBe('gzip');
  });
});
