import { test, expect } from '@playwright/test';
import { randomBytes } from 'crypto';

// L2-028: Digital Assets API contract tests
// These tests verify the /api/digital-assets endpoints conform to the expected contract.
// They are expected to FAIL until the API is implemented.

const API_BASE_URL = process.env.API_BASE_URL || 'http://localhost:5001';

const validCredentials = {
  email: 'admin@blog.local',
  password: 'Admin123!',
};

test.describe('Digital Assets API - L2-028', () => {

  test('POST /api/digital-assets/upload with valid image returns 201', async ({ request }) => {
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

    // Create a minimal valid JPEG (starts with FFD8FF magic bytes)
    const jpegHeader = Buffer.from([0xFF, 0xD8, 0xFF, 0xE0]);
    const fakeImageContent = Buffer.concat([jpegHeader, randomBytes(1024)]);

    const response = await authContext.post('/api/digital-assets/upload', {
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
    expect(body.digitalAssetId).toBeTruthy();
    expect(body.url).toBeTruthy();

    await authContext.dispose();
  });

  test('POST /api/digital-assets/upload with non-image returns 400', async ({ request }) => {
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

    // Upload a non-image file (plain text pretending to be an executable)
    const nonImageContent = Buffer.from('This is not an image file. Just plain text.');

    const response = await authContext.post('/api/digital-assets/upload', {
      multipart: {
        file: {
          name: 'malicious.exe',
          mimeType: 'application/octet-stream',
          buffer: nonImageContent,
        },
      },
    });

    expect(response.status()).toBe(400);

    await authContext.dispose();
  });

  test('POST /api/digital-assets/upload with file > 10MB returns 413', async ({ request }) => {
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

    // Create a buffer slightly over 10MB
    const tenMBPlusOne = 10 * 1024 * 1024 + 1;
    const jpegHeader = Buffer.from([0xFF, 0xD8, 0xFF, 0xE0]);
    const oversizedContent = Buffer.concat([jpegHeader, Buffer.alloc(tenMBPlusOne)]);

    const response = await authContext.post('/api/digital-assets/upload', {
      multipart: {
        file: {
          name: 'oversized-image.jpg',
          mimeType: 'image/jpeg',
          buffer: oversizedContent,
        },
      },
    });

    expect(response.status()).toBe(413);

    await authContext.dispose();
  });

  test('POST /api/digital-assets/upload without auth returns 401', async ({ request }) => {
    const jpegHeader = Buffer.from([0xFF, 0xD8, 0xFF, 0xE0]);
    const fakeImageContent = Buffer.concat([jpegHeader, randomBytes(512)]);

    const response = await request.post('/api/digital-assets/upload', {
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

    const response = await authContext.get('/api/digital-assets');

    expect(response.status()).toBe(200);
    expect(response.headers()['content-type']).toContain('application/json');

    const body = await response.json();
    const items = Array.isArray(body) ? body : body.items;
    expect(Array.isArray(items)).toBe(true);

    // If there are items, verify their structure
    if (items.length > 0) {
      const asset = items[0];
      expect(asset.digitalAssetId).toBeTruthy();
      expect(asset.url).toBeTruthy();
    }

    await authContext.dispose();
  });
});
