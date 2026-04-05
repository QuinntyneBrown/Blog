import { test, expect } from '@playwright/test';

test.describe('CSP Reporting - L2-028', () => {
  test('CSP header includes report-uri directive pointing to /api/csp-report', async ({ page }) => {
    const response = await page.goto('/');

    const csp = response!.headers()['content-security-policy'];
    expect(csp).toBeDefined();
    expect(csp).toContain('report-uri /api/csp-report');
  });

  test('CSP header includes report-to directive', async ({ page }) => {
    const response = await page.goto('/');

    const csp = response!.headers()['content-security-policy'];
    expect(csp).toBeDefined();
    expect(csp).toContain('report-to csp-endpoint');
  });

  test('response includes Reporting-Endpoints header', async ({ page }) => {
    const response = await page.goto('/');

    const reportingEndpoints = response!.headers()['reporting-endpoints'];
    expect(reportingEndpoints).toBeDefined();
    expect(reportingEndpoints).toContain('csp-endpoint=');
  });

  test('POST /api/csp-report accepts violation report and returns 204', async ({ request }) => {
    const response = await request.post('/api/csp-report', {
      headers: {
        'Content-Type': 'application/csp-report',
      },
      data: JSON.stringify({
        'csp-report': {
          'document-uri': 'https://example.com',
          'violated-directive': "script-src 'self'",
          'original-policy': "default-src 'self'",
          'blocked-uri': 'https://evil.com/script.js',
        },
      }),
    });

    expect(response.status()).toBe(204);
  });

  test('POST /api/csp-report accepts JSON content type', async ({ request }) => {
    const response = await request.post('/api/csp-report', {
      headers: {
        'Content-Type': 'application/json',
      },
      data: JSON.stringify({
        type: 'csp-violation',
        body: {
          documentURL: 'https://example.com',
          violatedDirective: "script-src 'self'",
        },
      }),
    });

    expect(response.status()).toBe(204);
  });
});
