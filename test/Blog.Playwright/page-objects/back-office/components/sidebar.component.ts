import { type Locator } from '@playwright/test';

export class SidebarComponent {
  readonly root: Locator;
  readonly brand: Locator;
  readonly navItems: Locator;
  readonly articlesLink: Locator;
  readonly assetsLink: Locator;

  constructor(root: Locator) {
    this.root = root;
    this.brand = root.getByText('QB');
    this.navItems = root.getByRole('link');
    this.articlesLink = root.getByRole('link', { name: /articles/i });
    this.assetsLink = root.getByRole('link', { name: /assets/i });
  }

  async isVisible(): Promise<boolean> {
    return await this.root.isVisible();
  }

  async getActiveItem(): Promise<string> {
    return await this.root.locator('[aria-current="page"]').innerText();
  }
}
