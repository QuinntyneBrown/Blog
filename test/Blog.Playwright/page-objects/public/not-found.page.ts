import { type Page, type Locator } from '@playwright/test';

export class NotFoundPage {
  readonly page: Page;
  readonly heading: Locator;
  readonly message: Locator;
  readonly homeLink: Locator;

  constructor(page: Page) {
    this.page = page;
    this.heading = page.getByRole('heading', { name: /not found|404/i });
    this.message = page.locator('[data-testid="not-found-message"]');
    this.homeLink = page.getByRole('link', { name: /home|articles|back/i });
  }

  async isVisible(): Promise<boolean> {
    return await this.heading.isVisible();
  }

  async getMessageText(): Promise<string> {
    return await this.message.innerText();
  }
}
