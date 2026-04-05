import { Page, Locator } from '@playwright/test';

export class NavDesktopComponent {
  readonly page: Page;
  readonly logo: Locator;
  readonly articlesLink: Locator;
  readonly feedLink: Locator;
  readonly rssLink: Locator;

  constructor(page: Page) {
    this.page = page;
    this.logo = page.locator('.nav-logo');
    this.articlesLink = page.locator('.nav-links a[href="/articles"]');
    this.feedLink = page.locator('.nav-links a[href="/feed"]');
    this.rssLink = page.locator('.nav-links .rss-link');
  }

  async clickLogo() { await this.logo.click(); }
  async clickArticles() { await this.articlesLink.click(); }
  async clickFeed() { await this.feedLink.click(); }
}
