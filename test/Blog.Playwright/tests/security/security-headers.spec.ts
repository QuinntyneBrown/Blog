import { test, expect } from '@playwright/test';

test.describe('Security Headers - L2-026', () => {
  test('response includes Strict-Transport-Security with max-age >= 31536000', async ({ page }) => {
    const response = await page.goto('/');

    const hsts = response!.headers()['strict-transport-security'];
    expect(hsts).toBeDefined();

    const maxAgeMatch = hsts.match(/max-age=(\d+)/);
    expect(maxAgeMatch).not.toBeNull();
    expect(Number(maxAgeMatch![1])).toBeGreaterThanOrEqual(31536000);
  });

  test('response includes Content-Security-Policy header', async ({ page }) => {
    const response = await page.goto('/');

    const csp = response!.headers()['content-security-policy'];
    expect(csp).toBeDefined();
  });

  test('response includes X-Content-Type-Options: nosniff', async ({ page }) => {
    const response = await page.goto('/');

    const xcto = response!.headers()['x-content-type-options'];
    expect(xcto).toBe('nosniff');
  });

  test('response includes X-Frame-Options: DENY', async ({ page }) => {
    const response = await page.goto('/');

    const xfo = response!.headers()['x-frame-options'];
    expect(xfo).toBe('DENY');
  });

  test('response includes Referrer-Policy: strict-origin-when-cross-origin', async ({ page }) => {
    const response = await page.goto('/');

    const referrerPolicy = response!.headers()['referrer-policy'];
    expect(referrerPolicy).toBe('strict-origin-when-cross-origin');
  });

  test('response includes Permissions-Policy header', async ({ page }) => {
    const response = await page.goto('/');

    const permissionsPolicy = response!.headers()['permissions-policy'];
    expect(permissionsPolicy).toBeDefined();
  });
});
