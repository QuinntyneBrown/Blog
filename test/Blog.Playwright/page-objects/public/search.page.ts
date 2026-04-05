import { Page, Locator } from '@playwright/test';

export class SearchPage {
  readonly page: Page;
  readonly heroSection: Locator;
  readonly heroLabel: Locator;
  readonly heroBar: Locator;
  readonly heroInput: Locator;
  readonly heroClearButton: Locator;
  readonly resultCount: Locator;
  readonly filtersBar: Locator;
  readonly filterTagAll: Locator;
  readonly contentSection: Locator;
  readonly resultsGrid: Locator;
  readonly resultCards: Locator;
  readonly emptyState: Locator;
  readonly emptyHeading: Locator;
  readonly pagination: Locator;

  constructor(page: Page) {
    this.page = page;
    this.heroSection = page.locator('.search-hero');
    this.heroLabel = page.locator('.search-hero-label');
    this.heroBar = page.locator('.search-hero-bar');
    this.heroInput = page.locator('.search-hero-input');
    this.heroClearButton = page.locator('.search-hero-clear');
    this.resultCount = page.locator('.search-hero-count');
    this.filtersBar = page.locator('.search-filters-bar');
    this.filterTagAll = page.locator('.search-tag-active');
    this.contentSection = page.locator('section[aria-label="Search results"]');
    this.resultsGrid = page.locator('.search-results-grid');
    this.resultCards = page.locator('[data-testid="search-result"]');
    this.emptyState = page.locator('.search-empty-state');
    this.emptyHeading = page.locator('.empty-heading');
    this.pagination = page.locator('.pagination');
  }

  async goto(query?: string) {
    const url = query ? `/search?q=${encodeURIComponent(query)}` : '/search';
    await this.page.goto(url);
  }

  async submitSearch(query: string) {
    await this.heroInput.fill(query);
    await this.heroInput.press('Enter');
    await this.page.waitForURL(/\/search/);
  }

  async getResultCardCount() {
    return this.resultCards.count();
  }

  async getGridColumnCount(): Promise<number> {
    const gridStyle = await this.resultsGrid.evaluate((el) => {
      const computed = window.getComputedStyle(el);
      return computed.getPropertyValue('grid-template-columns');
    });
    // Count how many column widths are defined (split by spaces of column tracks)
    return gridStyle.trim().split(/\s+/).length;
  }

  async firstResultTitle(): Promise<string> {
    return (await this.resultCards.first().locator('.article-card-title').textContent()) ?? '';
  }

  async firstResultLink(): Promise<string> {
    return (await this.resultCards.first().locator('.article-card-link').getAttribute('href')) ?? '';
  }
}
