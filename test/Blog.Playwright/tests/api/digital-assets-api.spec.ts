import { test, expect } from '@playwright/test';
import { randomBytes } from 'crypto';
import { testUser } from '../../fixtures/test-data';

// L2-028: Digital Assets API contract tests

const API_BASE_URL = process.env.API_URL || 'http://localhost:5001';

async function getAuthToken(request: import('@playwright/test').APIRequestContext): Promise<string> {
  const loginResponse = await request.post(`${API_BASE_URL}/api/auth/login`, {
    data: { email: testUser.email, password: testUser.password },
  });
  const body = await loginResponse.json();
  return body.data.token;
}

test.describe('Digital Assets API - L2-028', () => {

  test('POST /api/digital-assets/upload with valid image returns 201', async ({ request }) => {
    const token = await getAuthToken(request);

    // Create a minimal valid JPEG (starts with FFD8FF magic bytes)
    const jpegHeader = Buffer.from([0xFF, 0xD8, 0xFF, 0xE0]);
    const fakeImageContent = Buffer.concat([jpegHeader, randomBytes(1024)]);

    const response = await request.post(`${API_BASE_URL}/api/digital-assets/upload`, {
      headers: { Authorization: `Bearer ${token}` },
      multipart: {
        file: {
          name: 'test-image.jpg',
          mimeType: 'image/jpeg',
          buffer: fakeImageContent,
        },
      },
    });

    expect(response.status()).toBe(201);

    const body = await response.json();
    const data = body.data || body;
    expect(data.digitalAssetId).toBeTruthy();
    expect(data.url).toBeTruthy();
  });

  test('POST /api/digital-assets/upload with non-image returns 400', async ({ request }) => {
    const token = await getAuthToken(request);

    // Upload a non-image file (plain text pretending to be an executable)
    const nonImageContent = Buffer.from('This is not an image file. Just plain text.');

    const response = await request.post(`${API_BASE_URL}/api/digital-assets/upload`, {
      headers: { Authorization: `Bearer ${token}` },
      multipart: {
        file: {
          name: 'malicious.exe',
          mimeType: 'application/octet-stream',
          buffer: nonImageContent,
        },
      },
    });

    expect(response.status()).toBe(400);
  });

  test('POST /api/digital-assets/upload with file > 10MB returns 413', async ({ request }) => {
    const token = await getAuthToken(request);

    // Create a buffer slightly over 10MB
    const tenMBPlusOne = 10 * 1024 * 1024 + 1;
    const jpegHeader = Buffer.from([0xFF, 0xD8, 0xFF, 0xE0]);
    const oversizedContent = Buffer.concat([jpegHeader, Buffer.alloc(tenMBPlusOne)]);

    const response = await request.post(`${API_BASE_URL}/api/digital-assets/upload`, {
      headers: { Authorization: `Bearer ${token}` },
      multipart: {
        file: {
          name: 'oversized-image.jpg',
          mimeType: 'image/jpeg',
          buffer: oversizedContent,
        },
      },
    });

    expect(response.status()).toBe(413);
  });

  test('POST /api/digital-assets/upload without auth returns 401', async ({ request }) => {
    const jpegHeader = Buffer.from([0xFF, 0xD8, 0xFF, 0xE0]);
    const fakeImageContent = Buffer.concat([jpegHeader, randomBytes(512)]);

    const response = await request.post(`${API_BASE_URL}/api/digital-assets/upload`, {
      multipart: {
        file: {
          name: 'test-image.jpg',
          mimeType: 'image/jpeg',
          buffer: fakeImageContent,
        },
      },
    });

    expect(response.status()).toBe(401);
  });

  test('GET /api/digital-assets returns list of uploaded assets', async ({ request }) => {
    const token = await getAuthToken(request);

    const response = await request.get(`${API_BASE_URL}/api/digital-assets`, {
      headers: { Authorization: `Bearer ${token}` },
    });

    expect(response.status()).toBe(200);
    expect(response.headers()['content-type']).toContain('application/json');

    const body = await response.json();
    const data = body.data || body;
    const items = Array.isArray(data) ? data : data.items;
    expect(Array.isArray(items)).toBe(true);

    // If there are items, verify their structure
    if (items.length > 0) {
      const asset = items[0];
      expect(asset.digitalAssetId).toBeTruthy();
      expect(asset.url).toBeTruthy();
    }
  });
});
