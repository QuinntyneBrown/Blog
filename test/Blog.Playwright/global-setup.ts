import { chromium, FullConfig } from '@playwright/test';
import { ApiClient } from './helpers/api-client';
import { TEST_ADMIN, createArticleData } from './fixtures/test-data';

async function globalSetup(config: FullConfig) {
  const api = new ApiClient();
  await api.init();

  try {
    await api.login(TEST_ADMIN.email, TEST_ADMIN.password);

    for (let i = 0; i < 5; i++) {
      const article = createArticleData({ title: `Seed Article ${i + 1}` });
      const created = await api.createArticle(article);
      if (i < 3) {
        await api.publishArticle(created.articleId);
      }
    }
  } catch {
    console.warn('Global setup: Could not seed test data (server may not be running).');
  } finally {
    await api.dispose();
  }

  const browser = await chromium.launch();
  const context = await browser.newContext({
    baseURL: process.env.API_BASE_URL || 'http://localhost:5001',
  });
  const page = await context.newPage();

  try {
    await page.goto('/login');
    await page.getByLabel('Email').fill(TEST_ADMIN.email);
    await page.getByLabel('Password').fill(TEST_ADMIN.password);
    await page.getByRole('button', { name: /sign in/i }).click();
    await page.waitForURL('**/articles');
    await context.storageState({ path: '.auth/admin.json' });
  } catch {
    console.warn('Global setup: Could not create auth storage state (server may not be running).');
  } finally {
    await browser.close();
  }
}

export default globalSetup;
