import { test, expect } from '@playwright/test';

test.describe('Health Checks', () => {
  test('/health returns healthy status', async ({ request }) => {
    const response = await request.get('/health');
    expect(response.status()).toBe(200);
  });

  test('/health/ready returns status object', async ({ request }) => {
    const response = await request.get('/health/ready');
    expect([200, 503]).toContain(response.status());
    const body = await response.json();
    expect(body).toHaveProperty('status');
  });
});
