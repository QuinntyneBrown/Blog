import { Page, Locator } from '@playwright/test';

export class PublicArticleDetailPage {
  readonly page: Page;
  readonly articleTitle: Locator;
  readonly articleBody: Locator;
  readonly articleMeta: Locator;
  readonly backLink: Locator;
  readonly featuredImage: Locator;

  constructor(page: Page) {
    this.page = page;
    this.articleTitle = page.locator('.article-title');
    this.articleBody = page.locator('.article-body');
    this.articleMeta = page.locator('.article-meta');
    this.backLink = page.locator('.back-link');
    this.featuredImage = page.locator('.article-featured-image img');
  }

  async goto(slug: string) {
    await this.page.goto(`/articles/${slug}`);
  }

  async getTitle() {
    return this.articleTitle.textContent();
  }

  async clickBack() {
    await this.backLink.click();
  }
}
