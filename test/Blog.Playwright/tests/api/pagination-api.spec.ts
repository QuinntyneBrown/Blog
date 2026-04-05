import { test, expect } from '@playwright/test';

// L2-031: Pagination API contract tests
// These tests verify the /api/posts pagination behavior conforms to the expected contract.
// They are expected to FAIL until the API is implemented.

test.describe('Pagination API - L2-031', () => {

  test('GET /api/posts?pageSize=20 returns max 20 items with pagination metadata', async ({ request }) => {
    const response = await request.get('/api/posts', {
      params: {
        page: 1,
        pageSize: 20,
      },
    });

    expect(response.status()).toBe(200);
    expect(response.headers()['content-type']).toContain('application/json');

    const body = await response.json();

    // Response should include pagination metadata
    expect(body.totalCount).toBeDefined();
    expect(typeof body.totalCount).toBe('number');
    expect(body.page).toBeDefined();
    expect(body.pageSize).toBeDefined();
    expect(body.totalPages).toBeDefined();

    // Items array should exist and not exceed requested page size
    expect(Array.isArray(body.items)).toBe(true);
    expect(body.items.length).toBeLessThanOrEqual(20);

    // Page size in metadata should match requested value
    expect(body.pageSize).toBe(20);
    expect(body.page).toBe(1);
  });

  test('GET /api/posts?pageSize=200 caps at 100 items', async ({ request }) => {
    const response = await request.get('/api/posts', {
      params: {
        page: 1,
        pageSize: 200,
      },
    });

    expect(response.status()).toBe(200);

    const body = await response.json();

    // Server should cap pageSize at 100 even if 200 is requested
    expect(body.pageSize).toBeLessThanOrEqual(100);
    expect(body.items.length).toBeLessThanOrEqual(100);
  });

  test('page 2 results do not overlap with page 1', async ({ request }) => {
    // Fetch page 1
    const page1Response = await request.get('/api/posts', {
      params: {
        page: 1,
        pageSize: 5,
      },
    });

    expect(page1Response.status()).toBe(200);
    const page1 = await page1Response.json();

    // Fetch page 2
    const page2Response = await request.get('/api/posts', {
      params: {
        page: 2,
        pageSize: 5,
      },
    });

    expect(page2Response.status()).toBe(200);
    const page2 = await page2Response.json();

    // Extract IDs from both pages
    const page1Ids = page1.items.map((item: { articleId: string }) => item.articleId);
    const page2Ids = page2.items.map((item: { articleId: string }) => item.articleId);

    // There should be no overlap between page 1 and page 2
    const overlap = page1Ids.filter((id: string) => page2Ids.includes(id));
    expect(overlap).toHaveLength(0);

    // Page metadata should reflect correct page numbers
    expect(page1.page).toBe(1);
    expect(page2.page).toBe(2);
  });

  test('response includes totalCount, page, pageSize, totalPages fields', async ({ request }) => {
    const response = await request.get('/api/posts', {
      params: {
        page: 1,
        pageSize: 10,
      },
    });

    expect(response.status()).toBe(200);

    const body = await response.json();

    // All required pagination fields must be present
    expect(body).toHaveProperty('totalCount');
    expect(body).toHaveProperty('page');
    expect(body).toHaveProperty('pageSize');
    expect(body).toHaveProperty('totalPages');

    // All fields must be numbers
    expect(typeof body.totalCount).toBe('number');
    expect(typeof body.page).toBe('number');
    expect(typeof body.pageSize).toBe('number');
    expect(typeof body.totalPages).toBe('number');

    // Logical consistency checks
    expect(body.totalCount).toBeGreaterThanOrEqual(0);
    expect(body.page).toBeGreaterThanOrEqual(1);
    expect(body.pageSize).toBeGreaterThanOrEqual(1);
    expect(body.totalPages).toBeGreaterThanOrEqual(0);

    // totalPages should be consistent with totalCount and pageSize
    const expectedTotalPages = Math.ceil(body.totalCount / body.pageSize);
    expect(body.totalPages).toBe(expectedTotalPages);
  });
});
