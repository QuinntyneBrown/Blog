import { Page, Locator } from '@playwright/test';

export class PaginationComponent {
  readonly page: Page;
  readonly prevButton: Locator;
  readonly nextButton: Locator;
  readonly pageNumbers: Locator;

  constructor(page: Page) {
    this.page = page;
    this.prevButton = page.locator('.pagination .pagination-btn').first();
    this.nextButton = page.locator('.pagination .pagination-btn').last();
    this.pageNumbers = page.locator('.pagination .pagination-page');
  }

  async getActivePage() {
    return this.pageNumbers.locator('.active').textContent();
  }

  async clickNext() { await this.nextButton.click(); }
  async clickPrev() { await this.prevButton.click(); }
}
