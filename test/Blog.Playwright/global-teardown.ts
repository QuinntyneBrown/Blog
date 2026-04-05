import { FullConfig } from '@playwright/test';
import { ApiClient } from './helpers/api-client';
import { TEST_ADMIN } from './fixtures/test-data';

async function globalTeardown(config: FullConfig) {
  const api = new ApiClient();
  await api.init();

  try {
    await api.login(TEST_ADMIN.email, TEST_ADMIN.password);
    const articles = await api.getArticles(1, 100);
    for (const article of articles.items) {
      if (article.title.startsWith('Test Article') || article.title.startsWith('Seed Article')) {
        await api.deleteArticle(article.articleId);
      }
    }
  } catch {
    console.warn('Global teardown: Could not clean up test data.');
  } finally {
    await api.dispose();
  }
}

export default globalTeardown;
