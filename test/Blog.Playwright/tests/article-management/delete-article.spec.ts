import { test, expect } from '../../fixtures/base.fixture';
import { createArticleData } from '../../fixtures/test-data';
import { ToastComponent } from '../../page-objects/back-office/components/toast.component';
import { ModalComponent } from '../../page-objects/back-office/components/modal.component';

test.describe('L2-004: Delete Article', () => {
  test('should show confirmation modal when clicking delete', async ({
    articleEditorPage,
  }) => {
    const pg = articleEditorPage.page;
    const toast = new ToastComponent(pg);
    const article = createArticleData();

    await articleEditorPage.goto();
    await articleEditorPage.fillArticle(article.title, article.body, article.abstract);
    await articleEditorPage.save();
    await toast.waitForSuccess();

    await articleEditorPage.delete();

    const modal = new ModalComponent(pg.locator('[data-testid="modal"]'));
    await expect(modal.root).toBeVisible();

    const headerText = await modal.getHeaderText();
    expect(headerText).toMatch(/confirm|delete/i);
  });

  test('should remove article from list and show success toast when confirming delete', async ({
    articleEditorPage,
    articleListPage,
  }) => {
    const pg = articleEditorPage.page;
    const toast = new ToastComponent(pg);
    const article = createArticleData();

    await articleEditorPage.goto();
    await articleEditorPage.fillArticle(article.title, article.body, article.abstract);
    await articleEditorPage.save();
    await toast.waitForSuccess();

    await articleEditorPage.delete();
    const modal = new ModalComponent(pg.locator('[data-testid="modal"]'));
    await modal.confirm();

    const deleteMessage = await toast.waitForSuccess();
    expect(deleteMessage).toContain('deleted');

    await articleListPage.goto();
    const rowCount = await articleListPage.getRowCount();

    for (let i = 0; i < rowCount; i++) {
      const row = articleListPage.getRow(i);
      const title = await row.getTitleText();
      expect(title).not.toBe(article.title);
    }
  });

  test('should keep article in list when canceling delete', async ({
    articleEditorPage,
    articleListPage,
  }) => {
    const pg = articleEditorPage.page;
    const toast = new ToastComponent(pg);
    const article = createArticleData();

    await articleEditorPage.goto();
    await articleEditorPage.fillArticle(article.title, article.body, article.abstract);
    await articleEditorPage.save();
    await toast.waitForSuccess();

    await articleEditorPage.delete();
    const modal = new ModalComponent(pg.locator('[data-testid="modal"]'));
    await modal.cancel();

    await expect(modal.root).not.toBeVisible();

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
  }) => {
    const pg = articleEditorPage.page;
    const toast = new ToastComponent(pg);
    const nonExistentId = 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee';

    await articleEditorPage.goto(nonExistentId);

    await articleEditorPage.delete();
    const modal = new ModalComponent(pg.locator('[data-testid="modal"]'));
    await modal.confirm();

    const errorMessage = await toast.waitForError();
    expect(errorMessage).toMatch(/not found|error/i);
  });
});
