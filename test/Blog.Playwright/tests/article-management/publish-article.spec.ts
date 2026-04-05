import { test, expect } from '../../fixtures/base.fixture';
import { createArticleData } from '../../fixtures/test-data';
import { ToastComponent } from '../../page-objects/back-office/components/toast.component';

test.describe('L2-003: Publish Article', () => {
  test('should change status badge to Published when publishing a draft article', async ({
    articleEditorPage,
    page,
  }) => {
    const toast = new ToastComponent(page);
    const article = createArticleData();

    // Create a draft article
    await articleEditorPage.goto();
    await articleEditorPage.fillArticle(article.title, article.body, article.abstract);
    await articleEditorPage.save();
    await toast.waitForSuccess();

    // Verify it starts as Draft
    const initialStatus = await articleEditorPage.getStatusText();
    expect(initialStatus).toBe('Draft');

    // Publish the article
    await articleEditorPage.publish();
    await toast.waitForSuccess();

    const publishedStatus = await articleEditorPage.getStatusText();
    expect(publishedStatus).toBe('Published');
  });

  test('should change status badge back to Draft when unpublishing a published article', async ({
    articleEditorPage,
    page,
  }) => {
    const toast = new ToastComponent(page);
    const article = createArticleData();

    // Create and publish an article
    await articleEditorPage.goto();
    await articleEditorPage.fillArticle(article.title, article.body, article.abstract);
    await articleEditorPage.save();
    await toast.waitForSuccess();
    await articleEditorPage.publish();
    await toast.waitForSuccess();

    const publishedStatus = await articleEditorPage.getStatusText();
    expect(publishedStatus).toBe('Published');

    // Unpublish the article
    await articleEditorPage.unpublish();
    await toast.waitForSuccess();

    const draftStatus = await articleEditorPage.getStatusText();
    expect(draftStatus).toBe('Draft');
  });

  test('should set datePublished when article is published', async ({
    articleEditorPage,
    page,
  }) => {
    const toast = new ToastComponent(page);
    const article = createArticleData();

    // Create and publish an article
    await articleEditorPage.goto();
    await articleEditorPage.fillArticle(article.title, article.body, article.abstract);
    await articleEditorPage.save();
    await toast.waitForSuccess();
    await articleEditorPage.publish();
    await toast.waitForSuccess();

    // Verify datePublished is displayed on the page
    const datePublished = page.locator('[data-testid="date-published"]');
    await expect(datePublished).toBeVisible();

    const dateText = await datePublished.innerText();
    expect(dateText).not.toBe('');
  });
});
