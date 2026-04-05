import { type Locator } from '@playwright/test';

export class TopBarComponent {
  readonly root: Locator;
  readonly heading: Locator;
  readonly actionButtons: Locator;
  readonly avatar: Locator;
  readonly hamburgerButton: Locator;

  constructor(root: Locator) {
    this.root = root;
    this.heading = root.getByRole('heading');
    this.actionButtons = root.getByRole('button');
    this.avatar = root.locator('[data-testid="user-avatar"]');
    this.hamburgerButton = root.getByRole('button', { name: /menu/i });
  }

  async getHeadingText(): Promise<string> {
    return await this.heading.innerText();
  }
}
