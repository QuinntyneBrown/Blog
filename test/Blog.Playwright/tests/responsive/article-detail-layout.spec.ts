import { test, expect } from '@playwright/test';
import { VIEWPORTS } from '../../helpers/viewport';
import { PublicArticleDetailPage } from '../../page-objects/public/article-detail.page';

const SAMPLE_SLUG = 'hello-world';

test.describe('L2-036: Article Detail – Responsive Layout', () => {
  let detailPage: PublicArticleDetailPage;

  test.beforeEach(async ({ page }) => {
    detailPage = new PublicArticleDetailPage(page);
    await detailPage.goto(SAMPLE_SLUG);
  });

  test('XL (>= 1200px): article body is constrained to max-width ~70ch and centered', async ({ page }) => {
    await page.setViewportSize(VIEWPORTS.XL);

    await expect(detailPage.body).toBeVisible();

    const bodyBox = await detailPage.body.boundingBox();
    expect(bodyBox).not.toBeNull();

    // 70ch is roughly 560-700px depending on font; body should not span the full viewport
    expect(bodyBox!.width).toBeLessThan(VIEWPORTS.XL.width - 100);

    // Body should be centered: left margin roughly equals right margin
    const leftMargin = bodyBox!.x;
    const rightMargin = VIEWPORTS.XL.width - (bodyBox!.x + bodyBox!.width);
    expect(Math.abs(leftMargin - rightMargin)).toBeLessThan(50);
  });

  test('XS (< 576px): body spans full width with at least 16px horizontal padding', async ({ page }) => {
    await page.setViewportSize(VIEWPORTS.XS);

    await expect(detailPage.body).toBeVisible();

    const bodyBox = await detailPage.body.boundingBox();
    expect(bodyBox).not.toBeNull();

    // Body should span close to full viewport width
    expect(bodyBox!.width).toBeGreaterThan(VIEWPORTS.XS.width - 80);

    // Horizontal padding: left and right margins should each be at least 16px
    const leftPadding = bodyBox!.x;
    const rightPadding = VIEWPORTS.XS.width - (bodyBox!.x + bodyBox!.width);
    expect(leftPadding).toBeGreaterThanOrEqual(16);
    expect(rightPadding).toBeGreaterThanOrEqual(16);
  });

  for (const [name, size] of Object.entries(VIEWPORTS)) {
    test(`${name} (${size.width}px): images scale to fit within container, never overflow`, async ({ page }) => {
      await page.setViewportSize(size);

      await expect(detailPage.body).toBeVisible();

      const bodyBox = await detailPage.body.boundingBox();
      expect(bodyBox).not.toBeNull();

      const images = detailPage.body.locator('img');
      const imageCount = await images.count();

      for (let i = 0; i < imageCount; i++) {
        const imgBox = await images.nth(i).boundingBox();
        if (imgBox) {
          expect(imgBox.width).toBeLessThanOrEqual(bodyBox!.width + 1);
          expect(imgBox.x).toBeGreaterThanOrEqual(bodyBox!.x - 1);
          expect(imgBox.x + imgBox.width).toBeLessThanOrEqual(bodyBox!.x + bodyBox!.width + 1);
        }
      }
    });

    test(`${name} (${size.width}px): base font size is at least 16px and line-height at least 1.5`, async ({ page }) => {
      await page.setViewportSize(size);

      await expect(detailPage.body).toBeVisible();

      const styles = await detailPage.body.evaluate((el) => {
        const computed = window.getComputedStyle(el);
        return {
          fontSize: parseFloat(computed.fontSize),
          lineHeight: parseFloat(computed.lineHeight),
        };
      });

      expect(styles.fontSize).toBeGreaterThanOrEqual(16);
      // lineHeight is in px; ratio = lineHeight / fontSize
      const lineHeightRatio = styles.lineHeight / styles.fontSize;
      expect(lineHeightRatio).toBeGreaterThanOrEqual(1.5);
    });
  }
});
