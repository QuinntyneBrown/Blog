import { Locator } from '@playwright/test';

export class SidebarComponent {
  readonly root: Locator;
  readonly articlesLink: Locator;
  readonly assetsLink: Locator;
  readonly settingsLink: Locator;
  readonly signOutButton: Locator;

  constructor(root: Locator) {
    this.root = root;
    this.articlesLink = root.locator('a[href="/admin/articles"]');
    this.assetsLink = root.locator('a[href="/admin/digital-assets"]');
    this.settingsLink = root.locator('a[href="/admin/settings"]');
    this.signOutButton = root.locator('button:has-text("Sign out")');
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
