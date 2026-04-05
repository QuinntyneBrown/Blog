import { Page, Locator } from '@playwright/test';

export class AdminArticleListPage {
  readonly page: Page;
  readonly newArticleButton: Locator;
  readonly tableRows: Locator;

  constructor(page: Page) {
    this.page = page;
    this.newArticleButton = page.locator('a[href="/admin/articles/create"]');
    this.tableRows = page.locator('tbody tr');
  }

  async goto() {
    await this.page.goto('/admin/articles');
  }

  async getRowCount() {
    return this.tableRows.count();
  }

  async getArticleTitles() {
    return this.tableRows.locator('.td-title').allTextContents();
  }

  async clickNewArticle() {
    await this.newArticleButton.click();
  }

  async getStatusBadge(rowIndex: number) {
    return this.tableRows.nth(rowIndex).locator('.badge').textContent();
  }
}
