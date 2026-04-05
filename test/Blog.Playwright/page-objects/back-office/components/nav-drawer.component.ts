import { type Locator } from '@playwright/test';

export class NavDrawerComponent {
  readonly root: Locator;
  readonly closeButton: Locator;
  readonly articlesLink: Locator;
  readonly mediaLink: Locator;
  readonly settingsLink: Locator;
  readonly signOutButton: Locator;

  constructor(root: Locator) {
    this.root = root;
    this.closeButton = root.locator('[data-testid="nav-drawer-close"]');
    this.articlesLink = root.locator('[data-testid="nav-drawer-articles-link"]');
    this.mediaLink = root.locator('[data-testid="nav-drawer-media-link"]');
    this.settingsLink = root.locator('[data-testid="nav-drawer-settings-link"]');
    this.signOutButton = root.locator('[data-testid="nav-drawer-signout"]');
  }
}
