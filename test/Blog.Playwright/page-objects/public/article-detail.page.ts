import { type Page, type Locator } from '@playwright/test';

export class PublicArticleDetailPage {
  readonly page: Page;
  readonly article: Locator;
  readonly title: Locator;
  readonly body: Locator;
  readonly featuredImage: Locator;
  readonly publishDate: Locator;
  readonly readingTime: Locator;
  readonly authorName: Locator;

  constructor(page: Page) {
    this.page = page;
    this.article = page.locator('article');
    this.title = page.getByRole('heading', { level: 1 });
    this.body = page.locator('[data-testid="article-body"]');
    this.featuredImage = page.locator('[data-testid="featured-image"]');
    this.publishDate = page.locator('time');
    this.readingTime = page.locator('[data-testid="reading-time"]');
    this.authorName = page.locator('[data-testid="author-name"]');
  }

  async goto(slug: string) {
    await this.page.goto(`/articles/${slug}`);
  }

  async getTitleText(): Promise<string> {
    return await this.title.innerText();
  }

  async getBodyHtml(): Promise<string> {
    return await this.body.innerHTML();
  }

  async getReadingTimeText(): Promise<string> {
    return await this.readingTime.innerText();
  }

  async getPublishDateText(): Promise<string> {
    return await this.publishDate.innerText();
  }
}
