import { test, expect } from '@playwright/test';

test.describe('Health Check - L2-032', () => {
  test('GET /health returns 200 with JSON body containing status: "healthy"', async ({ request }) => {
    const response = await request.get('/health');

    expect(response.status()).toBe(200);

    const body = await response.json();
    expect(body.status).toBe('healthy');
  });

  test('response Content-Type is application/json', async ({ request }) => {
    const response = await request.get('/health');

    const contentType = response.headers()['content-type'];
    expect(contentType).toContain('application/json');
  });
});
