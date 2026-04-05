import { test as base, Page } from '@playwright/test';
import { testUser } from './test-data';

type AuthFixtures = {
  authenticatedPage: Page;
};

export const test = base.extend<AuthFixtures>({
  authenticatedPage: async ({ page }, use) => {
    await page.goto('/admin/login');
    await page.fill('input[name="email"]', testUser.email);
    await page.fill('input[name="password"]', testUser.password);
    await page.click('button[type="submit"]');
    await page.waitForURL('/admin/articles');
    await use(page);
  },
});

export { expect } from '@playwright/test';
