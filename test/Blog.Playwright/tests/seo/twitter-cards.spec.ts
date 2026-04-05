import { test, expect } from '@playwright/test';

test.describe('L2-009: Twitter Cards', () => {
  const articleSlug = 'hello-world';
  const articleUrl = `/articles/${articleSlug}`;

  const requiredTwitterTags = [
    'twitter:card',
    'twitter:title',
    'twitter:description',
    'twitter:image',
  ];

  for (const tag of requiredTwitterTags) {
    test(`article page has ${tag} meta tag`, async ({ page }) => {
      await page.goto(articleUrl);

      const content = await page
        .locator(`meta[name="${tag}"]`)
        .getAttribute('content');
      expect(content).toBeTruthy();
    });
  }

  test('all twitter: tags have non-empty values', async ({ page }) => {
    await page.goto(articleUrl);

    const twitterTags = page.locator('meta[name^="twitter:"]');
    const count = await twitterTags.count();
    expect(count).toBeGreaterThan(0);

    for (let i = 0; i < count; i++) {
      const content = await twitterTags.nth(i).getAttribute('content');
      const name = await twitterTags.nth(i).getAttribute('name');
      expect(content, `${name} should have a non-empty value`).toBeTruthy();
    }
  });

  test('twitter:card value is "summary_large_image"', async ({ page }) => {
    await page.goto(articleUrl);

    const cardValue = await page
      .locator('meta[name="twitter:card"]')
      .getAttribute('content');
    expect(cardValue).toBe('summary_large_image');
  });
});
