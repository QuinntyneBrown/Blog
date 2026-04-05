import { type Page, type Locator } from '@playwright/test';

export class ToastComponent {
  readonly page: Page;

  constructor(page: Page) {
    this.page = page;
  }

  get success(): Locator {
    return this.page.locator('[data-testid="toast-success"]');
  }

  get error(): Locator {
    return this.page.locator('[data-testid="toast-error"]');
  }

  get warning(): Locator {
    return this.page.locator('[data-testid="toast-warning"]');
  }

  get info(): Locator {
    return this.page.locator('[data-testid="toast-info"]');
  }

  async waitForSuccess(): Promise<string> {
    await this.success.waitFor({ state: 'visible', timeout: 10000 });
    return await this.success.innerText();
  }

  async waitForError(): Promise<string> {
    await this.error.waitFor({ state: 'visible', timeout: 10000 });
    return await this.error.innerText();
  }
}
