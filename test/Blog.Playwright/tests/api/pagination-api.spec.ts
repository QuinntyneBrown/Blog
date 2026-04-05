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

test.describe('Pagination API - L2-031', () => {

  test('GET /api/posts?pageSize=20 returns max 20 items with pagination metadata', async ({ request }) => {
    const token = await getToken(request);
    const response = await request.get(`${API_URL}/api/articles`, {
      params: { page: 1, pageSize: 20 },
      headers: { Authorization: `Bearer ${token}` },
    });

    expect(response.status()).toBe(200);

    const raw = await response.json();
    const body = raw.data || raw;

    expect(body.totalCount).toBeDefined();
    expect(typeof body.totalCount).toBe('number');
    expect(body.page).toBeDefined();
    expect(body.pageSize).toBeDefined();
    expect(body.totalPages).toBeDefined();

    expect(Array.isArray(body.items)).toBe(true);
    expect(body.items.length).toBeLessThanOrEqual(20);

    expect(body.pageSize).toBe(20);
    expect(body.page).toBe(1);
  });

  test('GET /api/posts?pageSize=200 caps at 100 items', async ({ request }) => {
    const token = await getToken(request);
    const response = await request.get(`${API_URL}/api/articles`, {
      params: { page: 1, pageSize: 200 },
      headers: { Authorization: `Bearer ${token}` },
    });

    expect(response.status()).toBe(200);

    const raw = await response.json();
    const body = raw.data || raw;

    expect(body.pageSize).toBeLessThanOrEqual(100);
    expect(body.items.length).toBeLessThanOrEqual(100);
  });

  test('page 2 results do not overlap with page 1', async ({ request }) => {
    const token = await getToken(request);
    const page1Response = await request.get(`${API_URL}/api/articles`, {
      params: { page: 1, pageSize: 5 },
      headers: { Authorization: `Bearer ${token}` },
    });

    expect(page1Response.status()).toBe(200);
    const page1Raw = await page1Response.json();
    const page1 = page1Raw.data || page1Raw;

    const page2Response = await request.get(`${API_URL}/api/articles`, {
      params: { page: 2, pageSize: 5 },
      headers: { Authorization: `Bearer ${token}` },
    });

    expect(page2Response.status()).toBe(200);
    const page2Raw = await page2Response.json();
    const page2 = page2Raw.data || page2Raw;

    const page1Ids = page1.items.map((item: { articleId: string }) => item.articleId);
    const page2Ids = page2.items.map((item: { articleId: string }) => item.articleId);

    const overlap = page1Ids.filter((id: string) => page2Ids.includes(id));
    expect(overlap).toHaveLength(0);

    expect(page1.page).toBe(1);
    expect(page2.page).toBe(2);
  });

  test('response includes totalCount, page, pageSize, totalPages fields', async ({ request }) => {
    const token = await getToken(request);
    const response = await request.get(`${API_URL}/api/articles`, {
      params: { page: 1, pageSize: 10 },
      headers: { Authorization: `Bearer ${token}` },
    });

    expect(response.status()).toBe(200);

    const raw = await response.json();
    const body = raw.data || raw;

    expect(body).toHaveProperty('totalCount');
    expect(body).toHaveProperty('page');
    expect(body).toHaveProperty('pageSize');
    expect(body).toHaveProperty('totalPages');

    expect(typeof body.totalCount).toBe('number');
    expect(typeof body.page).toBe('number');
    expect(typeof body.pageSize).toBe('number');
    expect(typeof body.totalPages).toBe('number');

    expect(body.totalCount).toBeGreaterThanOrEqual(0);
    expect(body.page).toBeGreaterThanOrEqual(1);
    expect(body.pageSize).toBeGreaterThanOrEqual(1);
    expect(body.totalPages).toBeGreaterThanOrEqual(0);

    const expectedTotalPages = Math.ceil(body.totalCount / body.pageSize);
    expect(body.totalPages).toBe(expectedTotalPages);
  });
});
