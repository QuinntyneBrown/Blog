import { test, expect } from '@playwright/test';
import { VIEWPORTS } from '../../helpers/viewport';
import { PublicArticleListPage } from '../../page-objects/public/article-list.page';
import { FooterComponent } from '../../page-objects/public/components/footer.component';

test.describe('Footer – Responsive Layout', () => {
  let articleListPage: PublicArticleListPage;
  let footer: FooterComponent;

  test.beforeEach(async ({ page }) => {
    articleListPage = new PublicArticleListPage(page);
    footer = new FooterComponent(page.locator('footer'));
    await articleListPage.goto();
  });

  for (const [name, size] of Object.entries(VIEWPORTS)) {
    test(`${name} (${size.width}px): footer is visible`, async ({ page }) => {
      await page.setViewportSize(size);

      await expect(footer.root).toBeVisible();
      await expect(footer.copyright).toBeVisible();
    });
  }

  test.describe('small viewports: footer links stack vertically', () => {
    for (const [name, size] of Object.entries({ XS: VIEWPORTS.XS, SM: VIEWPORTS.SM })) {
      test(`${name} (${size.width}px): footer links are stacked vertically`, async ({ page }) => {
        await page.setViewportSize(size);

        await expect(footer.root).toBeVisible();

        const linkCount = await footer.getLinkCount();
        expect(linkCount).toBeGreaterThanOrEqual(2);

        // Verify vertical stacking: each link's Y position should be below the previous link
        for (let i = 1; i < linkCount; i++) {
          const prevBox = await footer.links.nth(i - 1).boundingBox();
          const currBox = await footer.links.nth(i).boundingBox();

          expect(prevBox).not.toBeNull();
          expect(currBox).not.toBeNull();

          // Current link should start at or below the bottom of the previous link
          expect(currBox!.y).toBeGreaterThanOrEqual(prevBox!.y + prevBox!.height - 2);
        }
      });
    }
  });

  test.describe('large viewports: footer links can be displayed inline', () => {
    for (const [name, size] of Object.entries({ LG: VIEWPORTS.LG, XL: VIEWPORTS.XL })) {
      test(`${name} (${size.width}px): footer links are displayed inline`, async ({ page }) => {
        await page.setViewportSize(size);

        await expect(footer.root).toBeVisible();

        const linkCount = await footer.getLinkCount();
        if (linkCount < 2) return;

        const firstBox = await footer.links.nth(0).boundingBox();
        const secondBox = await footer.links.nth(1).boundingBox();

        expect(firstBox).not.toBeNull();
        expect(secondBox).not.toBeNull();

        // Links should share the same Y (inline layout)
        expect(Math.abs(firstBox!.y - secondBox!.y)).toBeLessThan(5);
      });
    }
  });
});
