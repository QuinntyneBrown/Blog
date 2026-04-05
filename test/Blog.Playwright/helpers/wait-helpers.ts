import { Page } from '@playwright/test';

export async function waitForToast(page: Page, type: 'success' | 'error' | 'warning' | 'info') {
  await page.locator(`.toast-${type}`).waitFor({ state: 'visible', timeout: 5000 });
}

export async function waitForNavigation(page: Page) {
  await page.waitForLoadState('networkidle');
}
