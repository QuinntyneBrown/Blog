import { test, expect } from '@playwright/test';
import { VIEWPORTS } from '../../helpers/viewport';
import { SidebarComponent } from '../../page-objects/back-office/components/sidebar.component';
import { TopBarComponent } from '../../page-objects/back-office/components/top-bar.component';
import { NavDrawerComponent } from '../../page-objects/back-office/components/nav-drawer.component';
import * as path from 'path';

const BACK_OFFICE_URL = '/admin/articles';
const STORAGE_STATE_PATH = path.join(__dirname, '../../.auth-state.json');

test.describe('Back Office – Responsive Layout', () => {
  let sidebar: SidebarComponent;
  let topBar: TopBarComponent;
  let navDrawer: NavDrawerComponent;

  test.use({ storageState: STORAGE_STATE_PATH });

  test.beforeEach(async ({ page }) => {
    sidebar = new SidebarComponent(page.locator('[data-testid="sidebar"]'));
    topBar = new TopBarComponent(page.locator('[data-testid="top-bar"]'));
    navDrawer = new NavDrawerComponent(page.locator('[data-testid="nav-drawer"]'));
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

  test.describe('small viewports (MD/SM/XS): nav drawer opens and contains all nav items', () => {
    for (const [name, size] of Object.entries({ MD: VIEWPORTS.MD, SM: VIEWPORTS.SM, XS: VIEWPORTS.XS })) {
      test(`${name} (${size.width}px): hamburger opens nav drawer with all nav links`, async ({ page }) => {
        await page.setViewportSize(size);

        await expect(navDrawer.root).toBeHidden();

        await topBar.hamburgerButton.click();

        await expect(navDrawer.root).toBeVisible();
        await expect(navDrawer.articlesLink).toBeVisible();
        await expect(navDrawer.mediaLink).toBeVisible();
        await expect(navDrawer.settingsLink).toBeVisible();
        await expect(navDrawer.signOutButton).toBeVisible();
      });

      test(`${name} (${size.width}px): close button dismisses the nav drawer`, async ({ page }) => {
        await page.setViewportSize(size);

        await topBar.hamburgerButton.click();
        await expect(navDrawer.root).toBeVisible();

        await navDrawer.closeButton.click();
        await expect(navDrawer.root).toBeHidden();
      });

      test(`${name} (${size.width}px): backdrop click dismisses the nav drawer`, async ({ page }) => {
        await page.setViewportSize(size);

        await topBar.hamburgerButton.click();
        await expect(navDrawer.root).toBeVisible();

        await page.locator('[data-testid="nav-drawer-backdrop"]').click({ position: { x: 5, y: 5 } });
        await expect(navDrawer.root).toBeHidden();
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
