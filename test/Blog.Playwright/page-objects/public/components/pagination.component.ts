import { type Locator } from '@playwright/test';

export class PaginationComponent {
  readonly root: Locator;
  readonly previousButton: Locator;
  readonly nextButton: Locator;
  readonly pageNumbers: Locator;

  constructor(root: Locator) {
    this.root = root;
    this.previousButton = root.getByRole('link', { name: /previous|prev/i });
    this.nextButton = root.getByRole('link', { name: /next/i });
    this.pageNumbers = root.getByRole('link').filter({ hasNotText: /previous|prev|next/i });
  }

  async isVisible(): Promise<boolean> {
    return await this.root.isVisible();
  }

  async goToNext() {
    await this.nextButton.click();
  }

  async goToPrevious() {
    await this.previousButton.click();
  }

  async goToPage(pageNum: number) {
    await this.root.getByRole('link', { name: String(pageNum), exact: true }).click();
  }

  async getCurrentPage(): Promise<string> {
    return await this.root.locator('[aria-current="page"]').innerText();
  }
}
