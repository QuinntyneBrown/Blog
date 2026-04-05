import { type Page, type Locator } from '@playwright/test';
import { ArticleCardComponent } from './components/article-card.component';
import { PaginationComponent } from './components/pagination.component';

export class PublicArticleListPage {
  readonly page: Page;
  readonly heading: Locator;
  readonly articleCards: Locator;
  readonly emptyState: Locator;
  readonly pagination: PaginationComponent;

  constructor(page: Page) {
    this.page = page;
    this.heading = page.getByRole('heading', { level: 1 });
    this.articleCards = page.locator('[data-testid="article-card"]');
    this.emptyState = page.getByTestId('empty-state');
    this.pagination = new PaginationComponent(page.getByRole('navigation', { name: /pagination/i }));
  }

  async goto(pageNum = 1) {
    const url = pageNum === 1 ? '/articles' : `/articles?page=${pageNum}`;
    await this.page.goto(url);
  }

  async getCardCount(): Promise<number> {
    return await this.articleCards.count();
  }

  getCard(index: number): ArticleCardComponent {
    return new ArticleCardComponent(this.articleCards.nth(index));
  }

  async getHeadingText(): Promise<string> {
    return await this.heading.innerText();
  }
}
