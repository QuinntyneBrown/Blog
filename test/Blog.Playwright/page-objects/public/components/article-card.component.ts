import { type Locator } from '@playwright/test';

export class ArticleCardComponent {
  readonly root: Locator;
  readonly image: Locator;
  readonly title: Locator;
  readonly abstract: Locator;
  readonly date: Locator;
  readonly readingTime: Locator;
  readonly link: Locator;

  constructor(root: Locator) {
    this.root = root;
    this.image = root.locator('img');
    this.title = root.locator('[data-testid="card-title"]');
    this.abstract = root.locator('[data-testid="card-abstract"]');
    this.date = root.locator('time');
    this.readingTime = root.locator('[data-testid="card-reading-time"]');
    this.link = root.getByRole('link');
  }

  async getTitleText(): Promise<string> {
    return await this.title.innerText();
  }

  async getAbstractText(): Promise<string> {
    return await this.abstract.innerText();
  }

  async click() {
    await this.link.first().click();
  }
}
