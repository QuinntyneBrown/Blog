import { test, expect } from '@playwright/test';
import { VIEWPORTS } from '../../helpers/viewport';
import { PublicArticleListPage } from '../../page-objects/public/article-list.page';

test.describe('L2-035: Article List – Responsive Grid Layout', () => {
  let articleListPage: PublicArticleListPage;

  test.beforeEach(async ({ page }) => {
    articleListPage = new PublicArticleListPage(page);
    await articleListPage.goto();
  });

  test('XL (>= 1200px): articles display in 3-column grid', async ({ page }) => {
    await page.setViewportSize(VIEWPORTS.XL);

    const cards = articleListPage.articleCards;
    await expect(cards.first()).toBeVisible();

    const firstBox = await cards.nth(0).boundingBox();
    const secondBox = await cards.nth(1).boundingBox();
    const thirdBox = await cards.nth(2).boundingBox();

    expect(firstBox).not.toBeNull();
    expect(secondBox).not.toBeNull();
    expect(thirdBox).not.toBeNull();

    // All three cards should be on the same row (same Y position, within tolerance)
    expect(Math.abs(firstBox!.y - secondBox!.y)).toBeLessThan(5);
    expect(Math.abs(secondBox!.y - thirdBox!.y)).toBeLessThan(5);

    // Each card should occupy roughly 1/3 of the viewport width
    const expectedColumnWidth = VIEWPORTS.XL.width / 3;
    expect(firstBox!.width).toBeLessThan(expectedColumnWidth + 50);
    expect(firstBox!.width).toBeGreaterThan(expectedColumnWidth - 100);
  });

  for (const [name, size] of Object.entries({ MD: VIEWPORTS.MD, LG: VIEWPORTS.LG })) {
    test(`${name} (>= 768px, < 1200px): articles display in 2-column grid`, async ({ page }) => {
      await page.setViewportSize(size);

      const cards = articleListPage.articleCards;
      await expect(cards.first()).toBeVisible();

      const firstBox = await cards.nth(0).boundingBox();
      const secondBox = await cards.nth(1).boundingBox();
      const thirdBox = await cards.nth(2).boundingBox();

      expect(firstBox).not.toBeNull();
      expect(secondBox).not.toBeNull();
      expect(thirdBox).not.toBeNull();

      // First two cards should be on the same row
      expect(Math.abs(firstBox!.y - secondBox!.y)).toBeLessThan(5);

      // Third card should be on a new row (below the first two)
      expect(thirdBox!.y).toBeGreaterThan(firstBox!.y + firstBox!.height - 5);

      // Each card should occupy roughly half the viewport width
      const expectedColumnWidth = size.width / 2;
      expect(firstBox!.width).toBeLessThan(expectedColumnWidth + 50);
      expect(firstBox!.width).toBeGreaterThan(expectedColumnWidth - 100);
    });
  }

  test('XS (< 576px): articles display in single column', async ({ page }) => {
    await page.setViewportSize(VIEWPORTS.XS);

    const cards = articleListPage.articleCards;
    await expect(cards.first()).toBeVisible();

    const firstBox = await cards.nth(0).boundingBox();
    const secondBox = await cards.nth(1).boundingBox();

    expect(firstBox).not.toBeNull();
    expect(secondBox).not.toBeNull();

    // Cards should be stacked vertically (second card below the first)
    expect(secondBox!.y).toBeGreaterThan(firstBox!.y + firstBox!.height - 5);

    // Card width should be close to viewport width (minus padding)
    expect(firstBox!.width).toBeGreaterThan(VIEWPORTS.XS.width - 60);
  });

  for (const [name, size] of Object.entries(VIEWPORTS)) {
    test(`${name} (${size.width}px): no horizontal scrollbar`, async ({ page }) => {
      await page.setViewportSize(size);

      const scrollWidth = await page.evaluate(() => document.documentElement.scrollWidth);
      const clientWidth = await page.evaluate(() => document.documentElement.clientWidth);

      expect(scrollWidth).toBeLessThanOrEqual(clientWidth);
    });

    test(`${name} (${size.width}px): all text is readable without zooming (font-size >= 14px)`, async ({ page }) => {
      await page.setViewportSize(size);

      const cards = articleListPage.articleCards;
      await expect(cards.first()).toBeVisible();

      const cardCount = await cards.count();
      for (let i = 0; i < Math.min(cardCount, 3); i++) {
        const fontSize = await cards.nth(i).evaluate(
          (el) => parseFloat(window.getComputedStyle(el).fontSize),
        );
        expect(fontSize).toBeGreaterThanOrEqual(14);
      }
    });
  }
});
