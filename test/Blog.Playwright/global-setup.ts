import { chromium, FullConfig } from '@playwright/test';
import * as path from 'path';

const STORAGE_STATE_PATH = path.join(__dirname, '.auth-state.json');

async function globalSetup(config: FullConfig) {
  const { baseURL } = config.projects[0].use;
  const browser = await chromium.launch();
  const page = await browser.newPage();

  // Wait for the application to be ready
  let retries = 30;
  while (retries-- > 0) {
    try {
      await page.goto(`${baseURL}/health`, { timeout: 5000 });
      const text = await page.textContent('body');
      if (text?.includes('healthy')) break;
    } catch {
      await new Promise(r => setTimeout(r, 2000));
    }
  }

  // Login and save storage state for reuse by admin tests
  const email = process.env.ADMIN_EMAIL || 'admin@blog.dev';
  const password = process.env.ADMIN_PASSWORD || 'Admin1234!';

  await page.goto(`${baseURL}/admin/login`);
  await page.locator('input[name="email"]').fill(email);
  await page.locator('input[name="password"]').fill(password);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL(/\/admin\/articles/, { timeout: 30000 });

  // Save authenticated state
  await page.context().storageState({ path: STORAGE_STATE_PATH });

  await browser.close();
}

export default globalSetup;
