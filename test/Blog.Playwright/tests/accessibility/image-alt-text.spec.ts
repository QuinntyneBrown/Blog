import { test, expect } from '@playwright/test';

test.describe('Image Alt Text - L2-039', () => {
  test('all <img> elements have non-empty alt attribute', async ({ page }) => {
    await page.goto('/');

    const images = page.locator('img');
    const count = await images.count();
    expect(count).toBeGreaterThan(0);

    for (let i = 0; i < count; i++) {
      const img = images.nth(i);
      const alt = await img.getAttribute('alt');
      const role = await img.getAttribute('role');

      if (role === 'presentation') {
        continue;
      }

      expect(alt, `Image ${i} should have a non-empty alt attribute`).not.toBeNull();
      expect(alt!.trim().length, `Image ${i} alt text should not be empty`).toBeGreaterThan(0);
    }
  });

  test('decorative images have alt="" and role="presentation"', async ({ page }) => {
    await page.goto('/');

    const decorativeImages = page.locator('img[role="presentation"], img[aria-hidden="true"]');
    const count = await decorativeImages.count();

    for (let i = 0; i < count; i++) {
      const img = decorativeImages.nth(i);
      const alt = await img.getAttribute('alt');
      const role = await img.getAttribute('role');

      expect(alt, `Decorative image ${i} should have alt=""`).toBe('');
      expect(role, `Decorative image ${i} should have role="presentation"`).toBe('presentation');
    }
  });
});
