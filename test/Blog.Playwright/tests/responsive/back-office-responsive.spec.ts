import { test, expect } from '@playwright/test';
import { VIEWPORTS } from '../../helpers/viewport';
import { SidebarComponent } from '../../page-objects/back-office/components/sidebar.component';
import { TopBarComponent } from '../../page-objects/back-office/components/top-bar.component';

const BACK_OFFICE_URL = '/admin/articles';

test.describe('Back Office – Responsive Layout', () => {
  let sidebar: SidebarComponent;
  let topBar: TopBarComponent;

  test.beforeEach(async ({ page }) => {
    sidebar = new SidebarComponent(page.locator('[data-testid="sidebar"]'));
    topBar = new TopBarComponent(page.locator('[data-testid="top-bar"]'));
    await page.goto(BACK_OFFICE_URL);
  });

  test.describe('large viewports (XL/LG): sidebar is visible', () => {
    for (const [name, size] of Object.entries({ XL: VIEWPORTS.XL, LG: VIEWPORTS.LG })) {
      test(`${name} (${size.width}px): sidebar nav is visible`, async ({ page }) => {
        await page.setViewportSize(size);

        await expect(sidebar.root).toBeVisible();
        await expect(sidebar.articlesLink).toBeVisible();
        await expect(sidebar.assetsLink).toBeVisible();
      });
    }
  });

  test.describe('small viewports (MD/SM/XS): sidebar is hidden, hamburger appears', () => {
    for (const [name, size] of Object.entries({ MD: VIEWPORTS.MD, SM: VIEWPORTS.SM, XS: VIEWPORTS.XS })) {
      test(`${name} (${size.width}px): sidebar is hidden`, async ({ page }) => {
        await page.setViewportSize(size);

        await expect(sidebar.root).toBeHidden();
      });

      test(`${name} (${size.width}px): hamburger button appears in top bar`, async ({ page }) => {
        await page.setViewportSize(size);

        await expect(topBar.hamburgerButton).toBeVisible();
      });
    }
  });

  test.describe('small viewports (SM/XS): article list shows cards instead of table rows', () => {
    for (const [name, size] of Object.entries({ SM: VIEWPORTS.SM, XS: VIEWPORTS.XS })) {
      test(`${name} (${size.width}px): article cards are displayed`, async ({ page }) => {
        await page.setViewportSize(size);

        const articleCards = page.locator('[data-testid="article-card"]');
        const tableRows = page.locator('[data-testid="article-row"]');

        await expect(articleCards.first()).toBeVisible();

        const cardCount = await articleCards.count();
        expect(cardCount).toBeGreaterThan(0);

        // Table rows should not be visible on small viewports
        const rowCount = await tableRows.count();
        if (rowCount > 0) {
          await expect(tableRows.first()).toBeHidden();
        }
      });

      test(`${name} (${size.width}px): article cards have no horizontal overflow`, async ({ page }) => {
        await page.setViewportSize(size);

        const articleCards = page.locator('[data-testid="article-card"]');
        await expect(articleCards.first()).toBeVisible();

        const cardCount = await articleCards.count();
        for (let i = 0; i < Math.min(cardCount, 3); i++) {
          const cardBox = await articleCards.nth(i).boundingBox();
          expect(cardBox).not.toBeNull();
          expect(cardBox!.x).toBeGreaterThanOrEqual(0);
          expect(cardBox!.x + cardBox!.width).toBeLessThanOrEqual(size.width + 1);
        }
      });
    }
  });
});
