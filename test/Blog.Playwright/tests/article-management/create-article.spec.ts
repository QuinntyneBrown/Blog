import { test, expect } from '../../fixtures/base.fixture';
import { createArticleData } from '../../fixtures/test-data';
import { ToastComponent } from '../../page-objects/back-office/components/toast.component';

test.describe('L2-001: Create Article', () => {
  test('should create an article with title, body, and abstract and show success toast', async ({
    articleEditorPage,
    page,
  }) => {
    const toast = new ToastComponent(page);
    const article = createArticleData();

    await articleEditorPage.goto();
    await articleEditorPage.fillArticle(article.title, article.body, article.abstract);
    await articleEditorPage.save();

    const message = await toast.waitForSuccess();
    expect(message).toContain('saved');
  });

  test('should auto-generate slug from the title', async ({
    articleEditorPage,
  }) => {
    const article = createArticleData({ title: 'My Test Article' });

    await articleEditorPage.goto();
    await articleEditorPage.fillArticle(article.title, article.body, article.abstract);
    await articleEditorPage.save();

    const slug = await articleEditorPage.getSlugText();
    expect(slug).toBe('my-test-article');
  });

  test('should navigate back to article list after creating an article', async ({
    articleEditorPage,
    page,
  }) => {
    const toast = new ToastComponent(page);
    const article = createArticleData();

    await articleEditorPage.goto();
    await articleEditorPage.fillArticle(article.title, article.body, article.abstract);
    await articleEditorPage.save();
    await toast.waitForSuccess();

    await expect(page).toHaveURL(/\/articles$/);
  });

  test('should show new article in the article list with Draft status', async ({
    articleEditorPage,
    articleListPage,
    page,
  }) => {
    const toast = new ToastComponent(page);
    const article = createArticleData();

    await articleEditorPage.goto();
    await articleEditorPage.fillArticle(article.title, article.body, article.abstract);
    await articleEditorPage.save();
    await toast.waitForSuccess();

    await articleListPage.goto();

    const rowCount = await articleListPage.getRowCount();
    expect(rowCount).toBeGreaterThan(0);

    const firstRow = articleListPage.getRow(0);
    const title = await firstRow.getTitleText();
    const status = await firstRow.getStatusText();

    expect(title).toBe(article.title);
    expect(status).toBe('Draft');
  });
});
