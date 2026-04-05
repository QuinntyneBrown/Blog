import { type Locator } from '@playwright/test';

export class TableRowComponent {
  readonly root: Locator;
  readonly title: Locator;
  readonly statusBadge: Locator;
  readonly date: Locator;
  readonly editButton: Locator;
  readonly deleteButton: Locator;

  constructor(root: Locator) {
    this.root = root;
    this.title = root.locator('[data-testid="article-title"]');
    this.statusBadge = root.locator('[data-testid="status-badge"]');
    this.date = root.locator('[data-testid="article-date"]');
    this.editButton = root.getByRole('button', { name: /edit/i });
    this.deleteButton = root.getByRole('button', { name: /delete/i });
  }

  async getTitleText(): Promise<string> {
    return await this.title.innerText();
  }

  async getStatusText(): Promise<string> {
    return await this.statusBadge.innerText();
  }

  async clickEdit() {
    await this.editButton.click();
  }

  async clickDelete() {
    await this.deleteButton.click();
  }
}
