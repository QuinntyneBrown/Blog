import { type Locator } from '@playwright/test';

export class FooterComponent {
  readonly root: Locator;
  readonly links: Locator;
  readonly copyright: Locator;

  constructor(root: Locator) {
    this.root = root;
    this.links = root.getByRole('link');
    this.copyright = root.locator('[data-testid="copyright"]');
  }

  async isVisible(): Promise<boolean> {
    return await this.root.isVisible();
  }

  async getCopyrightText(): Promise<string> {
    return await this.copyright.innerText();
  }

  async getLinkCount(): Promise<number> {
    return await this.links.count();
  }
}
