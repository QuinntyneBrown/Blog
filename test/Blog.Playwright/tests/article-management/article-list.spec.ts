import { test, expect } from '../../fixtures/base.fixture';
import { createArticleData } from '../../fixtures/test-data';
import { ToastComponent } from '../../page-objects/back-office/components/toast.component';

test.describe('Back-office: Article List', () => {
  test('should render the article list page with heading "Articles"', async ({
    articleListPage,
  }) => {
    await articleListPage.goto();

    await expect(articleListPage.heading).toBeVisible();
    await expect(articleListPage.heading).toHaveText('Articles');
  });

  test('should display title, status badge, and date in table rows', async ({
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

    const firstRow = articleListPage.getRow(0);

    await expect(firstRow.title).toBeVisible();
    await expect(firstRow.statusBadge).toBeVisible();
    await expect(firstRow.date).toBeVisible();
  });

  test('should display a visible New Article button', async ({
    articleListPage,
  }) => {
    await articleListPage.goto();

    await expect(articleListPage.newArticleButton).toBeVisible();
  });

  test('should support pagination when many articles exist', async ({
    articleEditorPage,
    articleListPage,
  }) => {
    const toast = new ToastComponent(articleEditorPage.page);

    // Create enough articles to trigger pagination (assumes page size of 20)
    for (let i = 0; i < 22; i++) {
      const article = createArticleData({ title: `Pagination Test Article ${i}` });
      await articleEditorPage.goto();
      await articleEditorPage.fillArticle(article.title, article.body, article.abstract);
      await articleEditorPage.save();
      await toast.waitForSuccess();
    }

    await articleListPage.goto();

    await expect(articleListPage.paginationNext).toBeVisible();

    await articleListPage.paginationNext.click();

    const rowCount = await articleListPage.getRowCount();
    expect(rowCount).toBeGreaterThan(0);
  });
});
