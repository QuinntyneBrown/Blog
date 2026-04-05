import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

test.describe('Color Contrast - L2-039', () => {
  test('no color contrast violations on article listing page', async ({ page }) => {
    await page.goto('/');

    const results = await new AxeBuilder({ page })
      .withRules(['color-contrast'])
      .analyze();

    expect(results.violations).toHaveLength(0);
  });

  test('no color contrast violations on article detail page', async ({ page }) => {
    await page.goto('/articles/test-article');

    const results = await new AxeBuilder({ page })
      .withRules(['color-contrast'])
      .analyze();

    expect(results.violations).toHaveLength(0);
  });
});
