import { test, expect } from '@playwright/test';

test.describe('Keyboard Navigation - L2-039', () => {
  test('all nav links are reachable via Tab key', async ({ page }) => {
    await page.goto('/');

    const navLinks = page.locator('nav a');
    const count = await navLinks.count();
    expect(count).toBeGreaterThan(0);

    for (let i = 0; i < count; i++) {
      await page.keyboard.press('Tab');
      const focused = page.locator(':focus');
      const tagName = await focused.evaluate((el) => el.tagName.toLowerCase());
      expect(tagName).toBe('a');
    }

    const lastFocusedHref = await page.locator(':focus').getAttribute('href');
    const lastNavHref = await navLinks.last().getAttribute('href');
    expect(lastFocusedHref).toBe(lastNavHref);
  });

  test('article card links are focusable and activatable via Enter', async ({ page }) => {
    await page.goto('/');

    const articleLinks = page.locator('article a, .article-card a');
    const count = await articleLinks.count();
    expect(count).toBeGreaterThan(0);

    const firstLink = articleLinks.first();
    await firstLink.focus();
    await expect(firstLink).toBeFocused();

    const href = await firstLink.getAttribute('href');
    await page.keyboard.press('Enter');
    await page.waitForURL(`**${href}`);

    expect(page.url()).toContain(href!);
  });

  test('pagination links are keyboard accessible', async ({ page }) => {
    await page.goto('/');

    const paginationLinks = page.locator('nav[aria-label="pagination"] a, .pagination a');
    const count = await paginationLinks.count();
    expect(count).toBeGreaterThan(0);

    for (let i = 0; i < count; i++) {
      const link = paginationLinks.nth(i);
      await link.focus();
      await expect(link).toBeFocused();
    }
  });

  test('focus is visible on interactive elements', async ({ page }) => {
    await page.goto('/');

    const interactive = page.locator('a, button, input, select, textarea, [tabindex="0"]');
    const count = await interactive.count();
    expect(count).toBeGreaterThan(0);

    for (let i = 0; i < Math.min(count, 5); i++) {
      const element = interactive.nth(i);
      await element.focus();

      const outlineStyle = await element.evaluate((el) => {
        const styles = window.getComputedStyle(el);
        return {
          outline: styles.outline,
          outlineWidth: styles.outlineWidth,
          boxShadow: styles.boxShadow,
        };
      });

      const hasVisibleFocus =
        (outlineStyle.outlineWidth !== '0px' && outlineStyle.outline !== 'none') ||
        outlineStyle.boxShadow !== 'none';

      expect(hasVisibleFocus, `Element ${i} should have visible focus indicator`).toBe(true);
    }
  });
});
