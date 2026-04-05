import { type Page, type Locator } from '@playwright/test';
import { TableRowComponent } from './components/table-row.component';

export class ArticleListPage {
  readonly page: Page;
  readonly heading: Locator;
  readonly newArticleButton: Locator;
  readonly searchInput: Locator;
  readonly tableRows: Locator;
  readonly emptyState: Locator;
  readonly paginationNext: Locator;
  readonly paginationPrev: Locator;

  constructor(page: Page) {
    this.page = page;
    this.heading = page.getByRole('heading', { name: 'Articles' });
    this.newArticleButton = page.getByRole('link', { name: /new/i });
    this.searchInput = page.getByPlaceholder(/search/i);
    this.tableRows = page.locator('[data-testid="article-row"]');
    this.emptyState = page.getByText(/no articles/i);
    this.paginationNext = page.getByRole('button', { name: /next/i });
    this.paginationPrev = page.getByRole('button', { name: /previous/i });
  }

  async goto() {
    await this.page.goto('/articles');
  }

  async getRowCount(): Promise<number> {
    return await this.tableRows.count();
  }

  getRow(index: number): TableRowComponent {
    return new TableRowComponent(this.tableRows.nth(index));
  }

  async clickNewArticle() {
    await this.newArticleButton.click();
  }

  async search(query: string) {
    await this.searchInput.fill(query);
  }
}
