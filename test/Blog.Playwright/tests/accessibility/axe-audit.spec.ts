import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

test.describe('Axe Accessibility Audit - L2-039', () => {
  test('article listing page has zero critical axe-core violations', async ({ page }) => {
    await page.goto('/');

    const results = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa'])
      .analyze();

    const critical = results.violations.filter((v) => v.impact === 'critical');
    expect(critical).toHaveLength(0);
  });

  test('article detail page has zero critical axe-core violations', async ({ page }) => {
    await page.goto('/articles/test-article');

    const results = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa'])
      .analyze();

    const critical = results.violations.filter((v) => v.impact === 'critical');
    expect(critical).toHaveLength(0);
  });

  test('zero serious axe-core violations on article listing page', async ({ page }) => {
    await page.goto('/');

    const results = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa'])
      .analyze();

    const serious = results.violations.filter((v) => v.impact === 'serious');
    expect(serious).toHaveLength(0);
  });

  test('zero serious axe-core violations on article detail page', async ({ page }) => {
    await page.goto('/articles/test-article');

    const results = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa'])
      .analyze();

    const serious = results.violations.filter((v) => v.impact === 'serious');
    expect(serious).toHaveLength(0);
  });
});
