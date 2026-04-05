import { test, expect } from '../../fixtures/base.fixture';
import { createArticleData } from '../../fixtures/test-data';
import { ToastComponent } from '../../page-objects/back-office/components/toast.component';

test.describe('L2-002: Edit Article', () => {
  test('should update the slug when the title is edited', async ({
    articleEditorPage,
    page,
  }) => {
    const toast = new ToastComponent(page);
    const article = createArticleData();

    // Create an article first
    await articleEditorPage.goto();
    await articleEditorPage.fillArticle(article.title, article.body, article.abstract);
    await articleEditorPage.save();
    await toast.waitForSuccess();

    // Edit the title
    const updatedTitle = 'Updated Article Title';
    await articleEditorPage.titleInput.clear();
    await articleEditorPage.titleInput.fill(updatedTitle);
    await articleEditorPage.save();
    await toast.waitForSuccess();

    const slug = await articleEditorPage.getSlugText();
    expect(slug).toBe('updated-article-title');
  });

  test('should preserve title when editing body and abstract', async ({
    articleEditorPage,
    page,
  }) => {
    const toast = new ToastComponent(page);
    const article = createArticleData();

    // Create an article first
    await articleEditorPage.goto();
    await articleEditorPage.fillArticle(article.title, article.body, article.abstract);
    await articleEditorPage.save();
    await toast.waitForSuccess();

    // Edit body and abstract only
    const updatedBody = '<p>Updated body content for the article.</p>';
    const updatedAbstract = 'Updated abstract content';
    await articleEditorPage.bodyEditor.clear();
    await articleEditorPage.bodyEditor.fill(updatedBody);
    await articleEditorPage.abstractInput.clear();
    await articleEditorPage.abstractInput.fill(updatedAbstract);
    await articleEditorPage.save();
    await toast.waitForSuccess();

    const titleValue = await articleEditorPage.titleInput.inputValue();
    expect(titleValue).toBe(article.title);
  });

  test('should show success toast when saving edits', async ({
    articleEditorPage,
    page,
  }) => {
    const toast = new ToastComponent(page);
    const article = createArticleData();

    await articleEditorPage.goto();
    await articleEditorPage.fillArticle(article.title, article.body, article.abstract);
    await articleEditorPage.save();
    await toast.waitForSuccess();

    // Edit and save again
    await articleEditorPage.abstractInput.clear();
    await articleEditorPage.abstractInput.fill('Edited abstract');
    await articleEditorPage.save();

    const message = await toast.waitForSuccess();
    expect(message).toContain('saved');
  });

  test('should show 404 when navigating to a non-existent article ID', async ({
    articleEditorPage,
    page,
  }) => {
    const nonExistentId = 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee';
    await articleEditorPage.goto(nonExistentId);

    await expect(page.getByText(/not found|404/i)).toBeVisible();
  });
});
