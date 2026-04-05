import { test, expect } from '@playwright/test';
import { testUser } from '../../fixtures/test-data';

test.describe('Auth API', () => {
  test('POST /api/auth/login returns token on valid credentials', async ({ request }) => {
    const response = await request.post('/api/auth/login', {
      data: { email: testUser.email, password: testUser.password },
    });
    expect(response.status()).toBe(200);
    const body = await response.json();
    expect(body.data).toHaveProperty('token');
    expect(body.data).toHaveProperty('expiresAt');
  });

  test('POST /api/auth/login returns 401 on invalid credentials', async ({ request }) => {
    const response = await request.post('/api/auth/login', {
      data: { email: 'nobody@example.com', password: 'wrongpassword123' },
    });
    expect(response.status()).toBe(401);
    const body = await response.json();
    expect(body).toHaveProperty('title');
    expect(body.status).toBe(401);
  });
});
