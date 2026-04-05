import { test, expect } from '@playwright/test';

// L2-030: Articles API contract tests
// These tests verify the /api/posts endpoints conform to the expected contract.
// They are expected to FAIL until the API is implemented.

const API_BASE_URL = process.env.API_BASE_URL || 'http://localhost:5001';

const validCredentials = {
  email: 'admin@blog.local',
  password: 'Admin123!',
};

const validArticle = {
  title: 'Contract Test Article',
  body: '<p>This is a test article body for API contract verification.</p>',
  abstract: 'A short summary of the contract test article.',
};

test.describe('Articles API - L2-030', () => {

  test('GET /api/posts returns 200 with JSON array of posts', async ({ request }) => {
    const response = await request.get('/api/posts');

    expect(response.status()).toBe(200);
    expect(response.headers()['content-type']).toContain('application/json');

    const body = await response.json();
    expect(Array.isArray(body.items ?? body)).toBe(true);
  });

  test('POST /api/posts with valid data returns 201 with Location header', async ({ request }) => {
    // Authenticate first
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

    const response = await authContext.post('/api/posts', {
      data: validArticle,
    });

    expect(response.status()).toBe(201);
    expect(response.headers()['location']).toBeTruthy();
    expect(response.headers()['location']).toMatch(/\/api\/posts\/.+/);

    const body = await response.json();
    expect(body.title).toBe(validArticle.title);
    expect(body.body).toBe(validArticle.body);
    expect(body.abstract).toBe(validArticle.abstract);
    expect(body.articleId).toBeTruthy();

    await authContext.dispose();
  });

  test('PUT /api/posts/{id} with valid data returns 200', async ({ request }) => {
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

    // Create an article first
    const createResponse = await authContext.post('/api/posts', {
      data: validArticle,
    });
    const created = await createResponse.json();

    // Update it
    const updatedData = {
      title: 'Updated Contract Test Article',
      body: '<p>Updated body content.</p>',
      abstract: 'Updated abstract.',
    };

    const response = await authContext.put(`/api/posts/${created.articleId}`, {
      data: updatedData,
    });

    expect(response.status()).toBe(200);

    const body = await response.json();
    expect(body.title).toBe(updatedData.title);
    expect(body.body).toBe(updatedData.body);
    expect(body.abstract).toBe(updatedData.abstract);

    await authContext.dispose();
  });

  test('DELETE /api/posts/{id} returns 204 No Content', async ({ request }) => {
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

    // Create an article to delete
    const createResponse = await authContext.post('/api/posts', {
      data: validArticle,
    });
    const created = await createResponse.json();

    // Delete it
    const response = await authContext.delete(`/api/posts/${created.articleId}`);

    expect(response.status()).toBe(204);

    // Verify body is empty for 204
    const text = await response.text();
    expect(text).toBe('');

    await authContext.dispose();
  });

  test('POST /api/posts without auth returns 401', async ({ request }) => {
    const response = await request.post('/api/posts', {
      data: validArticle,
    });

    expect(response.status()).toBe(401);
  });

  test('GET /api/posts/{id} for non-existent returns 404', async ({ request }) => {
    const nonExistentId = '00000000-0000-0000-0000-000000000000';

    const response = await request.get(`/api/posts/${nonExistentId}`);

    expect(response.status()).toBe(404);
  });
});
