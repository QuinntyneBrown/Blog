import { test, expect } from '@playwright/test';
import { VIEWPORTS } from '../../helpers/viewport';
import { PublicArticleListPage } from '../../page-objects/public/article-list.page';
import { NavDesktopComponent } from '../../page-objects/public/components/nav-desktop.component';
import { NavMobileComponent } from '../../page-objects/public/components/nav-mobile.component';

test.describe('L2-037: Navigation – Responsive Behaviour', () => {
  let articleListPage: PublicArticleListPage;
  let desktopNav: NavDesktopComponent;
  let mobileNav: NavMobileComponent;

  test.beforeEach(async ({ page }) => {
    articleListPage = new PublicArticleListPage(page);
    desktopNav = new NavDesktopComponent(page.locator('[data-testid="nav-desktop"]'));
    mobileNav = new NavMobileComponent(page.locator('[data-testid="nav-mobile"]'));
    await articleListPage.goto();
  });

  test.describe('desktop viewports (>= 768px)', () => {
    for (const [name, size] of Object.entries({ MD: VIEWPORTS.MD, LG: VIEWPORTS.LG, XL: VIEWPORTS.XL })) {
      test(`${name} (${size.width}px): desktop nav links are visible`, async ({ page }) => {
        await page.setViewportSize(size);

        await expect(desktopNav.root).toBeVisible();
        await expect(desktopNav.links.first()).toBeVisible();
      });

      test(`${name} (${size.width}px): hamburger button is hidden`, async ({ page }) => {
        await page.setViewportSize(size);

        await expect(mobileNav.hamburgerButton).toBeHidden();
      });
    }
  });

  test.describe('mobile viewports (< 768px)', () => {
    for (const [name, size] of Object.entries({ XS: VIEWPORTS.XS, SM: VIEWPORTS.SM })) {
      test(`${name} (${size.width}px): hamburger button is visible`, async ({ page }) => {
        await page.setViewportSize(size);

        await expect(mobileNav.hamburgerButton).toBeVisible();
      });

      test(`${name} (${size.width}px): desktop nav links are hidden`, async ({ page }) => {
        await page.setViewportSize(size);

        await expect(desktopNav.links.first()).toBeHidden();
      });

      test(`${name} (${size.width}px): tapping hamburger reveals mobile menu with links`, async ({ page }) => {
        await page.setViewportSize(size);

        await expect(mobileNav.menu).toBeHidden();
        await mobileNav.openMenu();

        await expect(mobileNav.menu).toBeVisible();
        const linkCount = await mobileNav.getMenuLinkCount();
        expect(linkCount).toBeGreaterThan(0);
      });

      test(`${name} (${size.width}px): mobile nav links have at least 44x44px touch targets`, async ({ page }) => {
        await page.setViewportSize(size);
        await mobileNav.openMenu();

        const linkCount = await mobileNav.getMenuLinkCount();
        for (let i = 0; i < linkCount; i++) {
          const link = mobileNav.menuLinks.nth(i);
          const box = await link.boundingBox();
          expect(box).not.toBeNull();
          expect(box!.width).toBeGreaterThanOrEqual(44);
          expect(box!.height).toBeGreaterThanOrEqual(44);
        }
      });
    }
  });
});
