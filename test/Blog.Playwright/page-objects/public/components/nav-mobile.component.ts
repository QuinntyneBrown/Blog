import { type Locator } from '@playwright/test';

export class NavMobileComponent {
  readonly root: Locator;
  readonly hamburgerButton: Locator;
  readonly menu: Locator;
  readonly menuLinks: Locator;
  readonly closeButton: Locator;

  constructor(root: Locator) {
    this.root = root;
    this.hamburgerButton = root.getByRole('button', { name: /menu|toggle/i });
    this.menu = root.locator('[data-testid="mobile-menu"]');
    this.menuLinks = root.locator('[data-testid="mobile-menu"] a');
    this.closeButton = root.getByRole('button', { name: /close/i });
  }

  async openMenu() {
    await this.hamburgerButton.click();
  }

  async closeMenu() {
    await this.closeButton.click();
  }

  async isMenuVisible(): Promise<boolean> {
    return await this.menu.isVisible();
  }

  async getMenuLinkCount(): Promise<number> {
    return await this.menuLinks.count();
  }
}
