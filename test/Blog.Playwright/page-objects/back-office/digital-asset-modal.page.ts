import { type Page, type Locator } from '@playwright/test';

export class DigitalAssetModalPage {
  readonly page: Page;
  readonly modal: Locator;
  readonly dropzone: Locator;
  readonly fileInput: Locator;
  readonly preview: Locator;
  readonly uploadButton: Locator;
  readonly cancelButton: Locator;
  readonly errorMessage: Locator;
  readonly fileName: Locator;
  readonly fileSize: Locator;

  constructor(page: Page) {
    this.page = page;
    this.modal = page.getByRole('dialog');
    this.dropzone = page.locator('[data-testid="dropzone"]');
    this.fileInput = page.locator('input[type="file"]');
    this.preview = page.locator('[data-testid="upload-preview"]');
    this.uploadButton = page.getByRole('button', { name: /upload/i });
    this.cancelButton = page.getByRole('button', { name: /cancel/i });
    this.errorMessage = page.locator('[data-testid="upload-error"]');
    this.fileName = page.locator('[data-testid="file-name"]');
    this.fileSize = page.locator('[data-testid="file-size"]');
  }

  async selectFile(filePath: string) {
    await this.fileInput.setInputFiles(filePath);
  }

  async upload() {
    await this.uploadButton.click();
  }

  async cancel() {
    await this.cancelButton.click();
  }

  async getErrorText(): Promise<string> {
    return await this.errorMessage.innerText();
  }
}
