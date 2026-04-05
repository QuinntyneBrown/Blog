import { Page, Locator } from '@playwright/test';

export class PublicArticleListPage {
  readonly page: Page;
  readonly articleCards: Locator;
  readonly heroTitle: Locator;
  readonly emptyState: Locator;
  readonly pagination: Locator;

  constructor(page: Page) {
    this.page = page;
    this.articleCards = page.locator('.article-card');
    this.heroTitle = page.locator('.hero-title');
    this.emptyState = page.locator('.empty-state');
    this.pagination = page.locator('.pagination');
  }

  async goto() {
    await this.page.goto('/');
  }

  async gotoArticles() {
    await this.page.goto('/articles');
  }

  async getCardCount() {
    return this.articleCards.count();
  }

  async getCardTitles() {
    return this.articleCards.locator('.article-card-title').allTextContents();
  }

  async clickCard(index: number) {
    await this.articleCards.nth(index).locator('.article-card-link').click();
  }
}
