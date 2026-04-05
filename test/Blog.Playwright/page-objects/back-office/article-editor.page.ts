import { type Page, type Locator } from '@playwright/test';

export class ArticleEditorPage {
  readonly page: Page;
  readonly titleInput: Locator;
  readonly bodyEditor: Locator;
  readonly abstractInput: Locator;
  readonly saveButton: Locator;
  readonly publishButton: Locator;
  readonly unpublishButton: Locator;
  readonly deleteButton: Locator;
  readonly statusBadge: Locator;
  readonly slugDisplay: Locator;
  readonly featuredImageButton: Locator;
  readonly featuredImagePreview: Locator;
  readonly removeFeaturedImageButton: Locator;
  readonly backButton: Locator;
  readonly validationErrors: Locator;

  constructor(page: Page) {
    this.page = page;
    this.titleInput = page.getByLabel('Title');
    this.bodyEditor = page.getByLabel('Body');
    this.abstractInput = page.getByLabel('Abstract');
    this.saveButton = page.getByRole('button', { name: /save/i });
    this.publishButton = page.getByRole('button', { name: /publish/i });
    this.unpublishButton = page.getByRole('button', { name: /unpublish/i });
    this.deleteButton = page.getByRole('button', { name: /delete/i });
    this.statusBadge = page.locator('[data-testid="status-badge"]');
    this.slugDisplay = page.locator('[data-testid="slug-display"]');
    this.featuredImageButton = page.getByRole('button', { name: /featured image/i });
    this.featuredImagePreview = page.locator('[data-testid="featured-image-preview"]');
    this.removeFeaturedImageButton = page.getByRole('button', { name: /remove image/i });
    this.backButton = page.getByRole('link', { name: /back/i });
    this.validationErrors = page.locator('[data-testid="validation-error"]');
  }

  async goto(articleId?: string) {
    if (articleId) {
      await this.page.goto(`/articles/${articleId}/edit`);
    } else {
      await this.page.goto('/articles/new');
    }
  }

  async fillArticle(title: string, body: string, abstract: string) {
    await this.titleInput.fill(title);
    await this.bodyEditor.fill(body);
    await this.abstractInput.fill(abstract);
  }

  async save() {
    await this.saveButton.click();
  }

  async publish() {
    await this.publishButton.click();
  }

  async unpublish() {
    await this.unpublishButton.click();
  }

  async delete() {
    await this.deleteButton.click();
  }

  async getSlugText(): Promise<string> {
    return await this.slugDisplay.innerText();
  }

  async getStatusText(): Promise<string> {
    return await this.statusBadge.innerText();
  }

  async getValidationErrorTexts(): Promise<string[]> {
    const errors = await this.validationErrors.allInnerTexts();
    return errors;
  }
}
