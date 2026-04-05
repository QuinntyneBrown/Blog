import { test, expect } from '@playwright/test';
import { viewports } from '../../helpers/viewport';

/**
 * Header icon / element visibility across all breakpoints.
 *
 * Breakpoint rules (from _Layout.cshtml):
 *   > 991px  : .search-form visible, .search-toggle hidden
 *  ≤ 991px  : .search-form hidden,  .search-toggle visible
 *  ≤ 767px  : .nav-links hidden,    .nav-hamburger visible
 */

test.describe('Header icons — XL (1440px)', () => {
  test.beforeEach(async ({ page }) => {
    await page.setViewportSize(viewports.xl);
    await page.goto('/');
  });

  test('logo is visible', async ({ page }) => {
    await expect(page.locator('.nav-logo')).toBeVisible();
  });

  test('Articles and Feed nav links are visible', async ({ page }) => {
    await expect(page.locator('.nav-links a[href="/articles"]')).toBeVisible();
    await expect(page.locator('.nav-links a[href="/feed"]')).toBeVisible();
  });

  test('RSS icon link is visible', async ({ page }) => {
    await expect(page.locator('.nav-links .rss-link')).toBeVisible();
  });

  test('inline search form is visible', async ({ page }) => {
    await expect(page.locator('.search-form')).toBeVisible();
  });

  test('search toggle icon button is hidden', async ({ page }) => {
    await expect(page.locator('.search-toggle')).toBeHidden();
  });

  test('hamburger button is hidden', async ({ page }) => {
    await expect(page.locator('.nav-hamburger')).toBeHidden();
  });
});

test.describe('Header icons — LG (992px)', () => {
  test.beforeEach(async ({ page }) => {
    await page.setViewportSize(viewports.lg);
    await page.goto('/');
  });

  test('logo is visible', async ({ page }) => {
    await expect(page.locator('.nav-logo')).toBeVisible();
  });

  test('Articles and Feed nav links are visible', async ({ page }) => {
    await expect(page.locator('.nav-links a[href="/articles"]')).toBeVisible();
    await expect(page.locator('.nav-links a[href="/feed"]')).toBeVisible();
  });

  test('RSS icon link is visible', async ({ page }) => {
    await expect(page.locator('.nav-links .rss-link')).toBeVisible();
  });

  test('inline search form is visible', async ({ page }) => {
    await expect(page.locator('.search-form')).toBeVisible();
  });

  test('search toggle icon button is hidden', async ({ page }) => {
    await expect(page.locator('.search-toggle')).toBeHidden();
  });

  test('hamburger button is hidden', async ({ page }) => {
    await expect(page.locator('.nav-hamburger')).toBeHidden();
  });
});

test.describe('Header icons — MD (768px)', () => {
  test.beforeEach(async ({ page }) => {
    await page.setViewportSize(viewports.md);
    await page.goto('/');
  });

  test('logo is visible', async ({ page }) => {
    await expect(page.locator('.nav-logo')).toBeVisible();
  });

  test('Articles and Feed nav links are visible', async ({ page }) => {
    await expect(page.locator('.nav-links a[href="/articles"]')).toBeVisible();
    await expect(page.locator('.nav-links a[href="/feed"]')).toBeVisible();
  });

  test('inline search form is hidden (collapsed to icon)', async ({ page }) => {
    await expect(page.locator('.search-form')).toBeHidden();
  });

  test('search toggle icon button is visible', async ({ page }) => {
    await expect(page.locator('.search-toggle')).toBeVisible();
  });

  test('hamburger button is hidden', async ({ page }) => {
    await expect(page.locator('.nav-hamburger')).toBeHidden();
  });

  test('tapping search toggle expands the search form', async ({ page }) => {
    await page.locator('.search-toggle').click();
    await expect(page.locator('.search-form')).toBeVisible();
  });
});

test.describe('Header icons — SM (576px)', () => {
  test.beforeEach(async ({ page }) => {
    await page.setViewportSize(viewports.sm);
    await page.goto('/');
  });

  test('logo is visible', async ({ page }) => {
    await expect(page.locator('.nav-logo')).toBeVisible();
  });

  test('nav links are hidden', async ({ page }) => {
    await expect(page.locator('.nav-links')).toBeHidden();
  });

  test('inline search form is hidden', async ({ page }) => {
    await expect(page.locator('.search-form')).toBeHidden();
  });

  test('search toggle icon button is visible', async ({ page }) => {
    await expect(page.locator('.search-toggle')).toBeVisible();
  });

  test('hamburger button is visible', async ({ page }) => {
    await expect(page.locator('.nav-hamburger')).toBeVisible();
  });

  test('tapping hamburger reveals mobile menu with Articles, Feed, RSS links', async ({ page }) => {
    await page.locator('.nav-hamburger').click();
    const menu = page.locator('.mobile-menu');
    await expect(menu).toBeVisible();
    await expect(menu.locator('a[href="/articles"]')).toBeVisible();
    await expect(menu.locator('a[href="/feed"]')).toBeVisible();
    await expect(menu.locator('a[href="/feed.xml"]')).toBeVisible();
  });
});

test.describe('Header icons — XS (375px)', () => {
  test.beforeEach(async ({ page }) => {
    await page.setViewportSize(viewports.xs);
    await page.goto('/');
  });

  test('logo is visible', async ({ page }) => {
    await expect(page.locator('.nav-logo')).toBeVisible();
  });

  test('nav links are hidden', async ({ page }) => {
    await expect(page.locator('.nav-links')).toBeHidden();
  });

  test('inline search form is hidden', async ({ page }) => {
    await expect(page.locator('.search-form')).toBeHidden();
  });

  test('search toggle icon button is visible', async ({ page }) => {
    await expect(page.locator('.search-toggle')).toBeVisible();
  });

  test('hamburger button is visible', async ({ page }) => {
    await expect(page.locator('.nav-hamburger')).toBeVisible();
  });

  test('tapping hamburger reveals mobile menu with Articles, Feed, RSS links', async ({ page }) => {
    await page.locator('.nav-hamburger').click();
    const menu = page.locator('.mobile-menu');
    await expect(menu).toBeVisible();
    await expect(menu.locator('a[href="/articles"]')).toBeVisible();
    await expect(menu.locator('a[href="/feed"]')).toBeVisible();
    await expect(menu.locator('a[href="/feed.xml"]')).toBeVisible();
  });
});
