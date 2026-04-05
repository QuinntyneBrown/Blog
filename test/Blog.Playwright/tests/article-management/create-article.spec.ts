import { test, expect } from '../../fixtures/base.fixture';
import { createArticleData } from '../../fixtures/test-data';
import { ToastComponent } from '../../page-objects/back-office/components/toast.component';

test.describe('L2-001: Create Article', () => {
  test('should create an article with title, body, and abstract and show success toast', async ({
    articleEditorPage,
  }) => {
    const toast = new ToastComponent(articleEditorPage.page);
    const article = createArticleData();

    await articleEditorPage.goto();
    await articleEditorPage.fillArticle(article.title, article.body, article.abstract);
    await articleEditorPage.save();

    const message = await toast.waitForSuccess();
    expect(message).toMatch(/saved|created/i);
  });

  test('should auto-generate slug from the title', async ({
    articleEditorPage,
  }) => {
    const article = createArticleData({ title: 'My Test Article' });

    await articleEditorPage.goto();
    await articleEditorPage.fillArticle(article.title, article.body, article.abstract);
    await articleEditorPage.save();

    // Wait for redirect to Edit page where slug is displayed
    await articleEditorPage.page.waitForURL(/\/edit\//, { timeout: 10000 });
    await articleEditorPage.slugText.waitFor({ state: 'visible', timeout: 5000 });

    const slug = await articleEditorPage.getSlugText();
    expect(slug).toContain('my-test-article');
  });

  test('should navigate back to article list after creating an article', async ({
    articleEditorPage,
  }) => {
    const toast = new ToastComponent(articleEditorPage.page);
    const article = createArticleData();

    await articleEditorPage.goto();
    await articleEditorPage.fillArticle(article.title, article.body, article.abstract);
    await articleEditorPage.save();
    await toast.waitForSuccess();

    await expect(articleEditorPage.page).toHaveURL(/\/articles/);
  });

  test('should show new article in the article list with Draft status', async ({
    articleEditorPage,
    articleListPage,
  }) => {
    const toast = new ToastComponent(articleEditorPage.page);
    const article = createArticleData();

    await articleEditorPage.goto();
    await articleEditorPage.fillArticle(article.title, article.body, article.abstract);
    await articleEditorPage.save();
    await toast.waitForSuccess();

    await articleListPage.goto();

    const rowCount = await articleListPage.getRowCount();
    expect(rowCount).toBeGreaterThan(0);

    // Find the newly created article in the list
    let found = false;
    for (let i = 0; i < rowCount; i++) {
      const row = articleListPage.getRow(i);
      const rowTitle = await row.getTitleText();
      if (rowTitle === article.title) {
        const status = await row.getStatusText();
        expect(status).toBe('Draft');
        found = true;
        break;
      }
    }
    expect(found).toBe(true);
  });
});
