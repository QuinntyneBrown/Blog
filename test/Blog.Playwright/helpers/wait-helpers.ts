import { type Page, type Locator } from '@playwright/test';

export async function waitForToast(page: Page, type: 'success' | 'error' | 'warning' | 'info' = 'success'): Promise<Locator> {
  const toast = page.locator(`[data-testid="toast-${type}"]`);
  await toast.waitFor({ state: 'visible', timeout: 5000 });
  return toast;
}

export async function waitForNavigation(page: Page): Promise<void> {
  await page.waitForLoadState('networkidle');
}

export async function waitForTableLoad(page: Page): Promise<void> {
  await page.locator('[data-testid="article-row"]').first().waitFor({ state: 'visible', timeout: 10000 });
}

export async function waitForArticleCards(page: Page): Promise<void> {
  await page.locator('[data-testid="article-card"]').first().waitFor({ state: 'visible', timeout: 10000 });
}

export async function waitForModal(page: Page): Promise<Locator> {
  const modal = page.getByRole('dialog');
  await modal.waitFor({ state: 'visible', timeout: 5000 });
  return modal;
}

export async function waitForModalClosed(page: Page): Promise<void> {
  await page.getByRole('dialog').waitFor({ state: 'hidden', timeout: 5000 });
}
