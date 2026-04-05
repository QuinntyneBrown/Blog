import { chromium, FullConfig } from '@playwright/test';
import * as path from 'path';

const STORAGE_STATE_PATH = path.join(__dirname, '.auth-state.json');

async function globalSetup(config: FullConfig) {
  const { baseURL } = config.projects[0].use;
  const browser = await chromium.launch();
  const context = await browser.newContext();
  const page = await context.newPage();

  // Wait for the application to be ready
  let healthy = false;
  for (let i = 0; i < 30 && !healthy; i++) {
    try {
      const resp = await page.goto(`${baseURL}/health`, { timeout: 5000 });
      const text = await page.textContent('body');
      if (text?.includes('healthy')) healthy = true;
    } catch {
      await new Promise(r => setTimeout(r, 2000));
    }
  }

  // Login and save storage state for reuse by admin tests
  const email = process.env.ADMIN_EMAIL || 'admin@blog.dev';
  const password = process.env.ADMIN_PASSWORD || 'Admin1234!';

  let loginSuccess = false;
  for (let attempt = 0; attempt < 3 && !loginSuccess; attempt++) {
    try {
      await page.goto(`${baseURL}/admin/login`);
      await page.locator('input[name="email"]').fill(email);
      await page.locator('input[name="password"]').fill(password);
      await page.locator('button[type="submit"]').click();
      await page.waitForURL(/\/admin\/articles/, { timeout: 30000 });
      loginSuccess = true;
    } catch {
      console.log(`Login attempt ${attempt + 1} failed, retrying...`);
      await new Promise(r => setTimeout(r, 2000));
    }
  }

  if (loginSuccess) {
    await context.storageState({ path: STORAGE_STATE_PATH });
  }

  await browser.close();
}

export default globalSetup;
