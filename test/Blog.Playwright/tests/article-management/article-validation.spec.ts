import { test, expect } from '../../fixtures/base.fixture';
import { createArticleData, createArticleWithLongTitle } from '../../fixtures/test-data';
import { ToastComponent } from '../../page-objects/back-office/components/toast.component';

test.describe('L2-001/L2-002: Article Validation', () => {
  test('should show validation error when title is empty', async ({
    articleEditorPage,
  }) => {
    const article = createArticleData();

    await articleEditorPage.goto();
    await articleEditorPage.fillArticle('', article.body, article.abstract);
    await articleEditorPage.save();

    const errors = await articleEditorPage.getValidationErrorTexts();
    const hasTitleError = errors.some((error) => /title/i.test(error));
    expect(hasTitleError).toBe(true);
  });

  test('should show validation error when body is empty', async ({
    articleEditorPage,
  }) => {
    const article = createArticleData();

    await articleEditorPage.goto();
    await articleEditorPage.fillArticle(article.title, '', article.abstract);
    await articleEditorPage.save();

    const errors = await articleEditorPage.getValidationErrorTexts();
    const hasBodyError = errors.some((error) => /body/i.test(error));
    expect(hasBodyError).toBe(true);
  });

  test('should show validation error when title exceeds 200 characters', async ({
    articleEditorPage,
  }) => {
    const article = createArticleWithLongTitle();

    await articleEditorPage.goto();
    await articleEditorPage.fillArticle(article.title, article.body, article.abstract);
    await articleEditorPage.save();

    const errors = await articleEditorPage.getValidationErrorTexts();
    const hasLengthError = errors.some(
      (error) => /title/i.test(error) && /200|character|long|max/i.test(error),
    );
    expect(hasLengthError).toBe(true);
  });

  test('should show 409 conflict error toast when slug is a duplicate', async ({
    articleEditorPage,
    page,
  }) => {
    const toast = new ToastComponent(page);
    const fixedTitle = 'Duplicate Slug Article';

    // Create the first article with a specific title
    await articleEditorPage.goto();
    const firstArticle = createArticleData({ title: fixedTitle });
    await articleEditorPage.fillArticle(
      firstArticle.title,
      firstArticle.body,
      firstArticle.abstract,
    );
    await articleEditorPage.save();
    await toast.waitForSuccess();

    // Create a second article with the same title to trigger duplicate slug
    await articleEditorPage.goto();
    const secondArticle = createArticleData({ title: fixedTitle });
    await articleEditorPage.fillArticle(
      secondArticle.title,
      secondArticle.body,
      secondArticle.abstract,
    );
    await articleEditorPage.save();

    const errorMessage = await toast.waitForError();
    expect(errorMessage).toMatch(/conflict|duplicate|already exists/i);
  });
});
