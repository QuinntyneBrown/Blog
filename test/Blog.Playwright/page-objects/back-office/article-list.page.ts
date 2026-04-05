import { Page, Locator } from '@playwright/test';
import { TableRowComponent } from './components/table-row.component';

export class ArticleListPage {
  readonly page: Page;
  readonly heading: Locator;
  readonly newArticleButton: Locator;
  readonly searchInput: Locator;
  readonly tableRows: Locator;
  readonly paginationNext: Locator;

  constructor(page: Page) {
    this.page = page;
    this.heading = page.getByRole('heading', { name: 'Articles' });
    this.newArticleButton = page.locator('[data-testid="new-article-btn"]');
    this.searchInput = page.locator('[data-testid="articles-search"]');
    this.tableRows = page.locator('[data-testid="article-row"]');
    this.paginationNext = page.locator('[data-testid="pagination-next"]');
  }

  async goto() {
    await this.page.goto('/admin/articles');
  }

  getRow(index: number): TableRowComponent {
    return new TableRowComponent(this.tableRows.nth(index));
  }

  async getRowCount() {
    return this.tableRows.count();
  }

  async getArticleTitles() {
    return this.tableRows.locator('[data-testid="article-title"]').allTextContents();
  }

  async clickNewArticle() {
    await this.newArticleButton.click();
  }

  async getStatusBadge(rowIndex: number) {
    return this.tableRows.nth(rowIndex).locator('[data-testid="status-badge"]').textContent();
  }
}

/** @deprecated Use ArticleListPage */
export { ArticleListPage as AdminArticleListPage };
