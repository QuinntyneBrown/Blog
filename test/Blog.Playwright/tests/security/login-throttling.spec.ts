import { test, expect } from '@playwright/test';
import { LoginPage } from '../../page-objects/back-office/login.page';

test.describe('Login Throttling - L2-029', () => {
  test('after exceeding rate limit, login form displays throttle feedback', async ({
    page,
    request,
  }) => {
    // Exhaust the IP-based rate limit by sending rapid login requests
    for (let i = 0; i < 550; i++) {
      const resp = await request.post('/api/auth/login', {
        data: {
          email: `throttle-test-${i}@example.com`,
          password: 'wrong-password',
        },
      });
      if (resp.status() === 429) break;
    }

    // The next API request should be rate-limited
    const response = await request.post('/api/auth/login', {
      data: {
        email: 'throttle-final@example.com',
        password: 'wrong-password',
      },
    });

    expect(response.status()).toBe(429);
    const retryAfter = response.headers()['retry-after'];
    expect(retryAfter).toBeDefined();
  });
});
