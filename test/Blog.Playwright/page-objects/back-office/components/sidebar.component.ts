import { Page, Locator } from '@playwright/test';

export class SidebarComponent {
  readonly page: Page;
  readonly articlesLink: Locator;
  readonly assetsLink: Locator;
  readonly signOutButton: Locator;

  constructor(page: Page) {
    this.page = page;
    this.articlesLink = page.locator('a[href="/admin/articles"]');
    this.assetsLink = page.locator('a[href="/admin/digital-assets"]');
    this.signOutButton = page.locator('button:has-text("Sign out")');
  }

  async navigateToArticles() {
    await this.articlesLink.click();
  }

  async navigateToAssets() {
    await this.assetsLink.click();
  }

  async signOut() {
    await this.signOutButton.click();
  }
}
