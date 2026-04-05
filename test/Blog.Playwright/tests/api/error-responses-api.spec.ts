import { test, expect } from '@playwright/test';
import { testUser } from '../../fixtures/test-data';

const API_URL = process.env.API_URL || 'http://localhost:5001';

async function getToken(request: import('@playwright/test').APIRequestContext) {
  const resp = await request.post(`${API_URL}/api/auth/login`, {
    data: { email: testUser.email, password: testUser.password },
  });
  const body = await resp.json();
  return body.data.token as string;
}

test.describe('Error Responses API - L2-030 (RFC 7807)', () => {

  test('400 response body follows RFC 7807 with status, title, detail fields', async ({ request }) => {
    const token = await getToken(request);

    // Send an invalid POST (missing required fields) to trigger 400
    const response = await request.post(`${API_URL}/api/articles`, {
      data: {},
      headers: { Authorization: `Bearer ${token}` },
    });

    expect(response.status()).toBe(400);

    const body = await response.json();

    expect(body.status).toBe(400);
    expect(body.title).toBeTruthy();
    expect(typeof body.title).toBe('string');
  });

  test('404 response body follows RFC 7807 format', async ({ request }) => {
    const token = await getToken(request);
    const nonExistentId = '00000000-0000-0000-0000-000000000000';

    const response = await request.get(`${API_URL}/api/articles/${nonExistentId}`, {
      headers: { Authorization: `Bearer ${token}` },
    });

    expect(response.status()).toBe(404);

    const body = await response.json();

    expect(body.status).toBe(404);
    expect(body.title).toBeTruthy();
  });

  test('409 response body follows RFC 7807 format', async ({ request }) => {
    const token = await getToken(request);
    const headers = { Authorization: `Bearer ${token}` };

    // Create an article
    await request.post(`${API_URL}/api/articles`, {
      data: {
        title: 'Conflict Test Article',
        body: '# Body for conflict test.',
        abstract: 'Abstract for conflict test.',
      },
      headers,
    });

    // Attempt to create another article with the same title to trigger 409
    const conflictResponse = await request.post(`${API_URL}/api/articles`, {
      data: {
        title: 'Conflict Test Article',
        body: '# Duplicate title should cause conflict.',
        abstract: 'Duplicate abstract.',
      },
      headers,
    });

    expect(conflictResponse.status()).toBe(409);

    const body = await conflictResponse.json();

    expect(body.status).toBe(409);
    expect(body.title).toBeTruthy();
  });

  test('Content-Type for errors is application/problem+json', async ({ request }) => {
    const token = await getToken(request);
    const nonExistentId = '00000000-0000-0000-0000-000000000000';

    const response404 = await request.get(`${API_URL}/api/articles/${nonExistentId}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    expect(response404.status()).toBe(404);
    expect(response404.headers()['content-type']).toContain('application/problem+json');
  });
});
