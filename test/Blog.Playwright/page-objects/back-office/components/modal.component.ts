import { type Locator } from '@playwright/test';

export class ModalComponent {
  readonly root: Locator;
  readonly header: Locator;
  readonly body: Locator;
  readonly confirmButton: Locator;
  readonly cancelButton: Locator;
  readonly closeButton: Locator;

  constructor(root: Locator) {
    this.root = root;
    this.header = root.locator('[data-testid="modal-header"]');
    this.body = root.locator('[data-testid="modal-body"]');
    this.confirmButton = root.getByRole('button', { name: /confirm|delete|yes/i });
    this.cancelButton = root.getByRole('button', { name: /cancel|no/i });
    this.closeButton = root.getByRole('button', { name: /close/i });
  }

  async confirm() {
    await this.confirmButton.click();
  }

  async cancel() {
    await this.cancelButton.click();
  }

  async close() {
    await this.closeButton.click();
  }

  async getHeaderText(): Promise<string> {
    return await this.header.innerText();
  }
}
