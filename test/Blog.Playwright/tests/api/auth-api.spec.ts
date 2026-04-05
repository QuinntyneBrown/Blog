import { test, expect } from '@playwright/test';

// L2-023, L2-024: Authentication API contract tests
// These tests verify the /api/auth endpoints conform to the expected contract.
// They are expected to FAIL until the API is implemented.

const validCredentials = {
  email: 'admin@blog.local',
  password: 'Admin123!',
};

test.describe('Auth API - L2-023, L2-024', () => {

  test('POST /api/auth/login with valid credentials returns 200 with token', async ({ request }) => {
    const response = await request.post('/api/auth/login', {
      data: validCredentials,
    });

    expect(response.status()).toBe(200);
    expect(response.headers()['content-type']).toContain('application/json');

    const body = await response.json();
    expect(body.token).toBeTruthy();
    expect(typeof body.token).toBe('string');
    expect(body.token.length).toBeGreaterThan(0);
  });

  test('POST /api/auth/login with invalid password returns 401', async ({ request }) => {
    const response = await request.post('/api/auth/login', {
      data: {
        email: validCredentials.email,
        password: 'WrongPassword999!',
      },
    });

    expect(response.status()).toBe(401);
  });

  test('POST /api/auth/login with non-existent email returns 401', async ({ request }) => {
    const response = await request.post('/api/auth/login', {
      data: {
        email: 'nonexistent@blog.local',
        password: 'SomePassword123!',
      },
    });

    expect(response.status()).toBe(401);
  });

  test('error message is generic and does not reveal whether email or password was wrong', async ({ request }) => {
    // Test with wrong password
    const wrongPasswordResponse = await request.post('/api/auth/login', {
      data: {
        email: validCredentials.email,
        password: 'WrongPassword999!',
      },
    });

    // Test with wrong email
    const wrongEmailResponse = await request.post('/api/auth/login', {
      data: {
        email: 'nonexistent@blog.local',
        password: validCredentials.password,
      },
    });

    expect(wrongPasswordResponse.status()).toBe(401);
    expect(wrongEmailResponse.status()).toBe(401);

    const wrongPasswordBody = await wrongPasswordResponse.json();
    const wrongEmailBody = await wrongEmailResponse.json();

    // Both error messages must be identical to prevent user enumeration
    expect(wrongPasswordBody.message ?? wrongPasswordBody.detail).toBe(
      wrongEmailBody.message ?? wrongEmailBody.detail
    );

    // The message should not contain hints about which field was wrong
    const errorMessage = JSON.stringify(wrongPasswordBody).toLowerCase();
    expect(errorMessage).not.toContain('email not found');
    expect(errorMessage).not.toContain('user not found');
    expect(errorMessage).not.toContain('incorrect password');
    expect(errorMessage).not.toContain('wrong password');
    expect(errorMessage).not.toContain('password is invalid');
  });

  test('token is a valid JWT format (three dot-separated base64 segments)', async ({ request }) => {
    const response = await request.post('/api/auth/login', {
      data: validCredentials,
    });

    expect(response.status()).toBe(200);

    const body = await response.json();
    const token: string = body.token;

    // JWT must have exactly three dot-separated parts: header.payload.signature
    const parts = token.split('.');
    expect(parts).toHaveLength(3);

    // Each part must be a valid base64url string (alphanumeric, -, _, no padding required)
    const base64urlPattern = /^[A-Za-z0-9_-]+$/;
    expect(parts[0]).toMatch(base64urlPattern);
    expect(parts[1]).toMatch(base64urlPattern);
    expect(parts[2]).toMatch(base64urlPattern);

    // Header should decode to JSON with "alg" and "typ" fields
    const header = JSON.parse(Buffer.from(parts[0], 'base64url').toString());
    expect(header.alg).toBeTruthy();
    expect(header.typ).toBe('JWT');

    // Payload should decode to valid JSON
    const payload = JSON.parse(Buffer.from(parts[1], 'base64url').toString());
    expect(payload).toBeTruthy();
    expect(typeof payload).toBe('object');
  });
});
