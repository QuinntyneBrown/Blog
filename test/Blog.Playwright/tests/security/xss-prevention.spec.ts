import { test, expect } from '@playwright/test';

test.describe('XSS Prevention - L2-025', () => {
  test('article body with <script>alert("xss")</script> does not execute', async ({ page }) => {
    await page.goto('/articles/test-xss-article');

    const scriptTags = await page.locator('article script').count();
    expect(scriptTags).toBe(0);

    const bodyHtml = await page.locator('article .article-body').innerHTML();
    expect(bodyHtml).not.toContain('<script>');
    expect(bodyHtml).not.toContain('</script>');
  });

  test('article body with <img onerror="alert(\'xss\')"> has onerror stripped', async ({ page }) => {
    await page.goto('/articles/test-xss-article');

    const images = page.locator('article .article-body img');
    const count = await images.count();

    for (let i = 0; i < count; i++) {
      const onerror = await images.nth(i).getAttribute('onerror');
      expect(onerror).toBeNull();
    }
  });
});
