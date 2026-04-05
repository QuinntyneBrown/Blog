import { test as base } from '@playwright/test';
import { LoginPage } from '../page-objects/back-office/login.page';
import { TEST_ADMIN } from './test-data';

export const test = base.extend<{ authenticatedPage: LoginPage }>({
  authenticatedPage: async ({ page }, use) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login(TEST_ADMIN.email, TEST_ADMIN.password);
    await page.waitForURL('**/articles');
    await use(loginPage);
  },
});

export { expect } from '@playwright/test';
