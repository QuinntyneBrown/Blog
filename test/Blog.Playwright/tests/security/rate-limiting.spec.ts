import { test, expect } from '@playwright/test';

test.describe('Rate Limiting - L2-027', () => {
  test('rapid login attempts eventually return 429 status', async ({ request }) => {
    // Send requests until we get rate-limited (limit is configurable per environment)
    let got429 = false;
    for (let i = 0; i < 550; i++) {
      const response = await request.post('/api/auth/login', {
        data: {
          email: 'attacker@example.com',
          password: 'wrong-password',
        },
      });
      if (response.status() === 429) {
        got429 = true;
        break;
      }
    }
    expect(got429).toBe(true);
  });

  test('429 response includes Retry-After header', async ({ request }) => {
    // Exhaust the rate limit
    for (let i = 0; i < 550; i++) {
      const resp = await request.post('/api/auth/login', {
        data: {
          email: 'attacker2@example.com',
          password: 'wrong-password',
        },
      });
      if (resp.status() === 429) break;
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
