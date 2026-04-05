import { test, expect } from '@playwright/test';

test.describe('Rate Limiting - L2-027', () => {
  test('after 10 rapid login attempts, the 11th returns 429 status', async ({ request }) => {
    for (let i = 0; i < 10; i++) {
      await request.post('/api/auth/login', {
        data: {
          email: 'attacker@example.com',
          password: 'wrong-password',
        },
      });
    }

    const response = await request.post('/api/auth/login', {
      data: {
        email: 'attacker@example.com',
        password: 'wrong-password',
      },
    });

    expect(response.status()).toBe(429);
  });

  test('429 response includes Retry-After header', async ({ request }) => {
    for (let i = 0; i < 11; i++) {
      await request.post('/api/auth/login', {
        data: {
          email: 'attacker2@example.com',
          password: 'wrong-password',
        },
      });
    }

    const response = await request.post('/api/auth/login', {
      data: {
        email: 'attacker2@example.com',
        password: 'wrong-password',
      },
    });

    expect(response.status()).toBe(429);

    const retryAfter = response.headers()['retry-after'];
    expect(retryAfter).toBeDefined();
  });
});
