import { test, expect } from '../../fixtures/base.fixture';
import { createArticleData } from '../../fixtures/test-data';
import { ToastComponent } from '../../page-objects/back-office/components/toast.component';

test.describe('L2-003: Publish Article', () => {
  test('should change status badge to Published when publishing a draft article', async ({
    articleEditorPage,
  }) => {
    const toast = new ToastComponent(articleEditorPage.page);
    const article = createArticleData();

    await articleEditorPage.goto();
    await articleEditorPage.fillArticle(article.title, article.body, article.abstract);
    await articleEditorPage.save();
    await toast.waitForSuccess();

    const initialStatus = await articleEditorPage.getStatusText();
    expect(initialStatus).toBe('Draft');

    await articleEditorPage.publish();
    await toast.waitForSuccess();

    const publishedStatus = await articleEditorPage.getStatusText();
    expect(publishedStatus).toBe('Published');
  });

  test('should change status badge back to Draft when unpublishing a published article', async ({
    articleEditorPage,
  }) => {
    const toast = new ToastComponent(articleEditorPage.page);
    const article = createArticleData();

    await articleEditorPage.goto();
    await articleEditorPage.fillArticle(article.title, article.body, article.abstract);
    await articleEditorPage.save();
    await toast.waitForSuccess();
    await articleEditorPage.publish();
    await toast.waitForSuccess();

    const publishedStatus = await articleEditorPage.getStatusText();
    expect(publishedStatus).toBe('Published');

    await articleEditorPage.unpublish();
    await toast.waitForSuccess();

    const draftStatus = await articleEditorPage.getStatusText();
    expect(draftStatus).toBe('Draft');
  });

  test('should set datePublished when article is published', async ({
    articleEditorPage,
  }) => {
    const pg = articleEditorPage.page;
    const toast = new ToastComponent(pg);
    const article = createArticleData();

    await articleEditorPage.goto();
    await articleEditorPage.fillArticle(article.title, article.body, article.abstract);
    await articleEditorPage.save();
    await toast.waitForSuccess();
    await articleEditorPage.publish();
    await toast.waitForSuccess();

    const datePublished = pg.locator('[data-testid="date-published"]');
    await expect(datePublished).toBeAttached();

    const dateText = await datePublished.innerText();
    expect(dateText).not.toBe('');
  });
});
