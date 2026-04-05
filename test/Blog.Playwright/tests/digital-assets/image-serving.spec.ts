import { test, expect } from '../../fixtures/base.fixture';

test.describe('L2-020, L2-029: Image Serving – Responsive & Lazy Loading', () => {
  const PUBLISHED_SLUG = 'test-article-with-images';

  test.beforeEach(async ({ publicDetailPage }) => {
    await publicDetailPage.goto(PUBLISHED_SLUG);
  });

  test('public article images have srcset attribute for responsive images', async ({ page }) => {
    const images = page.locator('article img');
    const count = await images.count();
    expect(count).toBeGreaterThan(0);

    for (let i = 0; i < count; i++) {
      await expect(images.nth(i)).toHaveAttribute('srcset', /.+/);
    }
  });

  test('below-the-fold images have loading="lazy" and decoding="async"', async ({ page }) => {
    // Grab all article images except the first (which is typically above the fold)
    const allImages = page.locator('article img');
    const count = await allImages.count();
    expect(count).toBeGreaterThan(1);

    for (let i = 1; i < count; i++) {
      const img = allImages.nth(i);
      await expect(img).toHaveAttribute('loading', 'lazy');
      await expect(img).toHaveAttribute('decoding', 'async');
    }
  });

  test('featured image is present on article detail page', async ({ publicDetailPage }) => {
    await expect(publicDetailPage.featuredImage).toBeVisible();
    await expect(publicDetailPage.featuredImage).toHaveAttribute('src', /.+/);
  });
});
