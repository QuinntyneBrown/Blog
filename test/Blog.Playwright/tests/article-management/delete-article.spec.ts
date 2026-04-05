import { test, expect } from '../../fixtures/base.fixture';
import { createArticleData } from '../../fixtures/test-data';
import { ToastComponent } from '../../page-objects/back-office/components/toast.component';
import { ModalComponent } from '../../page-objects/back-office/components/modal.component';

test.describe('L2-004: Delete Article', () => {
  test('should show confirmation modal when clicking delete', async ({
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

    // Click delete
    await articleEditorPage.delete();

    // Verify the confirmation modal appears
    const modal = new ModalComponent(page.locator('[data-testid="modal"]'));
    await expect(modal.root).toBeVisible();

    const headerText = await modal.getHeaderText();
    expect(headerText).toMatch(/confirm|delete/i);
  });

  test('should remove article from list and show success toast when confirming delete', async ({
    articleEditorPage,
    articleListPage,
    page,
  }) => {
    const toast = new ToastComponent(page);
    const article = createArticleData();

    // Create an article first
    await articleEditorPage.goto();
    await articleEditorPage.fillArticle(article.title, article.body, article.abstract);
    await articleEditorPage.save();
    await toast.waitForSuccess();

    // Delete the article
    await articleEditorPage.delete();
    const modal = new ModalComponent(page.locator('[data-testid="modal"]'));
    await modal.confirm();

    const deleteMessage = await toast.waitForSuccess();
    expect(deleteMessage).toContain('deleted');

    // Verify article is no longer in the list
    await articleListPage.goto();
    const rowCount = await articleListPage.getRowCount();

    // Check that the deleted article title is not present in any row
    for (let i = 0; i < rowCount; i++) {
      const row = articleListPage.getRow(i);
      const title = await row.getTitleText();
      expect(title).not.toBe(article.title);
    }
  });

  test('should keep article in list when canceling delete', async ({
    articleEditorPage,
    articleListPage,
    page,
  }) => {
    const toast = new ToastComponent(page);
    const article = createArticleData();

    // Create an article first
    await articleEditorPage.goto();
    await articleEditorPage.fillArticle(article.title, article.body, article.abstract);
    await articleEditorPage.save();
    await toast.waitForSuccess();

    // Click delete then cancel
    await articleEditorPage.delete();
    const modal = new ModalComponent(page.locator('[data-testid="modal"]'));
    await modal.cancel();

    // Verify the modal is dismissed
    await expect(modal.root).not.toBeVisible();

    // Navigate to list and verify article still exists
    await articleListPage.goto();
    const rowCount = await articleListPage.getRowCount();
    expect(rowCount).toBeGreaterThan(0);

    let found = false;
    for (let i = 0; i < rowCount; i++) {
      const row = articleListPage.getRow(i);
      const title = await row.getTitleText();
      if (title === article.title) {
        found = true;
        break;
      }
    }
    expect(found).toBe(true);
  });

  test('should show error when deleting a non-existent article', async ({
    articleEditorPage,
    page,
  }) => {
    const toast = new ToastComponent(page);
    const nonExistentId = 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee';

    await articleEditorPage.goto(nonExistentId);

    // Attempt to delete a non-existent article
    await articleEditorPage.delete();
    const modal = new ModalComponent(page.locator('[data-testid="modal"]'));
    await modal.confirm();

    const errorMessage = await toast.waitForError();
    expect(errorMessage).toMatch(/not found|error/i);
  });
});
