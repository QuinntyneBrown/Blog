import { test, expect } from '@playwright/test';
import { ApiClient } from '../../helpers/api-client';
import { makeArticle, testUser } from '../../fixtures/test-data';

test.describe('Articles API', () => {
  test('GET /api/articles requires authentication', async ({ request }) => {
    const response = await request.get('/api/articles');
    expect(response.status()).toBe(401);
  });

  test('GET /api/articles returns paged response with valid token', async ({ request }) => {
    const client = new ApiClient(request);
    const token = await client.login(testUser.email, testUser.password);

    const response = await request.get('/api/articles', {
      headers: { Authorization: `Bearer ${token}` },
    });
    expect(response.status()).toBe(200);
    const body = await response.json();
    expect(body.data).toHaveProperty('items');
    expect(body.data).toHaveProperty('totalCount');
    expect(body.data).toHaveProperty('page');
  });

  test('POST /api/articles creates article and returns 201', async ({ request }) => {
    const client = new ApiClient(request);
    const token = await client.login(testUser.email, testUser.password);
    const article = makeArticle();

    const result = await client.createArticle(token, article);

    expect(result).toHaveProperty('articleId');
    expect(result.title).toBe(article.title);
    expect(result.published).toBe(false);

    // Cleanup
    await client.deleteArticle(token, result.articleId, result.version);
  });
});
