import { test, expect } from '@playwright/test';

test.describe('Image Optimization - L2-020', () => {
  test('images have srcset and sizes attributes', async ({ page }) => {
    await page.goto('/');

    const images = page.locator('img[src]');
    const count = await images.count();
    expect(count).toBeGreaterThan(0);

    for (let i = 0; i < count; i++) {
      const img = images.nth(i);
      const srcset = await img.getAttribute('srcset');
      const sizes = await img.getAttribute('sizes');

      expect(srcset, `Image ${i} should have srcset`).not.toBeNull();
      expect(sizes, `Image ${i} should have sizes`).not.toBeNull();
    }
  });

  test('below-fold images have loading="lazy" and decoding="async"', async ({ page }) => {
    await page.goto('/');

    const allImages = page.locator('img');
    const count = await allImages.count();
    expect(count).toBeGreaterThan(1);

    const viewportHeight = page.viewportSize()!.height;

    for (let i = 0; i < count; i++) {
      const img = allImages.nth(i);
      const box = await img.boundingBox();

      if (box && box.y > viewportHeight) {
        const loading = await img.getAttribute('loading');
        const decoding = await img.getAttribute('decoding');

        expect(loading, `Below-fold image ${i} should have loading="lazy"`).toBe('lazy');
        expect(decoding, `Below-fold image ${i} should have decoding="async"`).toBe('async');
      }
    }
  });

  test('request with Accept: image/webp gets WebP response', async ({ request }) => {
    const response = await request.get('/images/hero.jpg', {
      headers: {
        Accept: 'image/webp,image/*,*/*',
      },
    });

    const contentType = response.headers()['content-type'];
    expect(contentType).toContain('image/webp');
  });
});
