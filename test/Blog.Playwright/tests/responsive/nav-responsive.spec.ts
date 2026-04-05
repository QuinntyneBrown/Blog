import { test, expect } from '@playwright/test';
import { viewports } from '../../helpers/viewport';

test.describe('Responsive Navigation', () => {
  test('desktop nav is visible at XL viewport', async ({ page }) => {
    await page.setViewportSize(viewports.xl);
    await page.goto('/');
    await expect(page.locator('.nav-links')).toBeVisible();
    await expect(page.locator('.nav-hamburger')).toBeHidden();
  });

  test('hamburger menu shown at mobile viewport', async ({ page }) => {
    await page.setViewportSize(viewports.xs);
    await page.goto('/');
    await expect(page.locator('.nav-hamburger')).toBeVisible();
  });

  test('hamburger opens mobile menu', async ({ page }) => {
    await page.setViewportSize(viewports.xs);
    await page.goto('/');
    await page.locator('.nav-hamburger').click();
    await expect(page.locator('.mobile-menu')).toBeVisible();
  });
});
