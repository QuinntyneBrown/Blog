import { test, expect } from '@playwright/test';

// L2-030: Error Responses API contract tests
// These tests verify that error responses conform to RFC 7807 Problem Details format.
// They are expected to FAIL until the API is implemented.

const API_BASE_URL = process.env.API_BASE_URL || 'http://localhost:5001';

const validCredentials = {
  email: 'admin@blog.local',
  password: 'Admin123!',
};

test.describe('Error Responses API - L2-030 (RFC 7807)', () => {

  test('400 response body follows RFC 7807 with status, title, detail fields', async ({ request }) => {
    // Authenticate
    const loginResponse = await request.post('/api/auth/login', {
      data: validCredentials,
    });
    const { token } = await loginResponse.json();

    const authContext = await request.newContext({
      baseURL: API_BASE_URL,
      extraHTTPHeaders: {
        Authorization: `Bearer ${token}`,
      },
    });

    // Send an invalid POST (missing required fields) to trigger 400
    const response = await authContext.post('/api/posts', {
      data: {},
    });

    expect(response.status()).toBe(400);

    const body = await response.json();

    // RFC 7807 required fields
    expect(body.status).toBe(400);
    expect(body.title).toBeTruthy();
    expect(typeof body.title).toBe('string');
    expect(body.detail).toBeTruthy();
    expect(typeof body.detail).toBe('string');

    // Optional but recommended RFC 7807 fields
    if (body.type) {
      expect(typeof body.type).toBe('string');
    }
    if (body.instance) {
      expect(typeof body.instance).toBe('string');
    }

    await authContext.dispose();
  });

  test('404 response body follows RFC 7807 format', async ({ request }) => {
    const nonExistentId = '00000000-0000-0000-0000-000000000000';

    const response = await request.get(`/api/posts/${nonExistentId}`);

    expect(response.status()).toBe(404);

    const body = await response.json();

    // RFC 7807 required fields
    expect(body.status).toBe(404);
    expect(body.title).toBeTruthy();
    expect(typeof body.title).toBe('string');

    // detail is recommended but may be absent for 404
    if (body.detail) {
      expect(typeof body.detail).toBe('string');
    }

    // Optional but recommended RFC 7807 fields
    if (body.type) {
      expect(typeof body.type).toBe('string');
    }
    if (body.instance) {
      expect(typeof body.instance).toBe('string');
    }
  });

  test('409 response body follows RFC 7807 format', async ({ request }) => {
    // Authenticate
    const loginResponse = await request.post('/api/auth/login', {
      data: validCredentials,
    });
    const { token } = await loginResponse.json();

    const authContext = await request.newContext({
      baseURL: API_BASE_URL,
      extraHTTPHeaders: {
        Authorization: `Bearer ${token}`,
      },
    });

    // Create an article
    const createResponse = await authContext.post('/api/posts', {
      data: {
        title: 'Conflict Test Article',
        body: '<p>Body for conflict test.</p>',
        abstract: 'Abstract for conflict test.',
      },
    });
    const created = await createResponse.json();

    // Attempt to create another article with the same title to trigger 409
    const conflictResponse = await authContext.post('/api/posts', {
      data: {
        title: 'Conflict Test Article',
        body: '<p>Duplicate title should cause conflict.</p>',
        abstract: 'Duplicate abstract.',
      },
    });

    expect(conflictResponse.status()).toBe(409);

    const body = await conflictResponse.json();

    // RFC 7807 required fields
    expect(body.status).toBe(409);
    expect(body.title).toBeTruthy();
    expect(typeof body.title).toBe('string');
    expect(body.detail).toBeTruthy();
    expect(typeof body.detail).toBe('string');

    // Optional but recommended RFC 7807 fields
    if (body.type) {
      expect(typeof body.type).toBe('string');
    }
    if (body.instance) {
      expect(typeof body.instance).toBe('string');
    }

    // Clean up
    if (created.articleId) {
      await authContext.delete(`/api/posts/${created.articleId}`);
    }

    await authContext.dispose();
  });

  test('Content-Type for errors is application/problem+json', async ({ request }) => {
    const nonExistentId = '00000000-0000-0000-0000-000000000000';

    // Trigger a 404 error
    const response404 = await request.get(`/api/posts/${nonExistentId}`);
    expect(response404.status()).toBe(404);
    expect(response404.headers()['content-type']).toContain('application/problem+json');

    // Authenticate to trigger a 400 error
    const loginResponse = await request.post('/api/auth/login', {
      data: validCredentials,
    });
    const { token } = await loginResponse.json();

    const authContext = await request.newContext({
      baseURL: API_BASE_URL,
      extraHTTPHeaders: {
        Authorization: `Bearer ${token}`,
      },
    });

    // Trigger a 400 error with invalid data
    const response400 = await authContext.post('/api/posts', {
      data: {},
    });
    expect(response400.status()).toBe(400);
    expect(response400.headers()['content-type']).toContain('application/problem+json');

    await authContext.dispose();
  });
});
