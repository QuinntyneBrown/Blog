import { type Locator } from '@playwright/test';

export class NavDesktopComponent {
  readonly root: Locator;
  readonly logo: Locator;
  readonly links: Locator;
  readonly articlesLink: Locator;
  readonly rssLink: Locator;

  constructor(root: Locator) {
    this.root = root;
    this.logo = root.locator('[data-testid="site-logo"]');
    this.links = root.getByRole('link');
    this.articlesLink = root.getByRole('link', { name: /articles/i });
    this.rssLink = root.getByRole('link', { name: /rss|feed/i });
  }

  async isVisible(): Promise<boolean> {
    return await this.root.isVisible();
  }

  async getLinkCount(): Promise<number> {
    return await this.links.count();
  }
}
