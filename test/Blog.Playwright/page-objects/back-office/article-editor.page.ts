import { Page, Locator } from '@playwright/test';

export class ArticleEditorPage {
  readonly page: Page;
  readonly titleInput: Locator;
  readonly abstractInput: Locator;
  readonly bodyEditor: Locator;
  readonly saveDraftButton: Locator;
  readonly publishButton: Locator;
  readonly statusBadge: Locator;
  readonly slugText: Locator;
  readonly deleteButton: Locator;
  readonly featuredImageButton: Locator;
  readonly featuredImagePreview: Locator;
  readonly removeFeaturedImageButton: Locator;

  constructor(page: Page) {
    this.page = page;
    this.titleInput = page.locator('input[name="title"]');
    this.abstractInput = page.locator('textarea[name="abstract"]');
    this.bodyEditor = page.locator('textarea[name="body"]');
    this.saveDraftButton = page.locator('[data-testid="save-draft-btn"]');
    this.publishButton = page.locator('[data-testid="publish-btn"]');
    this.statusBadge = page.locator('[data-testid="toolbar-badge"]');
    this.slugText = page.locator('[data-testid="article-slug"]');
    this.deleteButton = page.locator('[data-testid="delete-btn"]');
    this.featuredImageButton = page.locator('[data-testid="featured-image-btn"]');
    this.featuredImagePreview = page.locator('[data-testid="featured-image-preview"]');
    this.removeFeaturedImageButton = page.locator('[data-testid="remove-image-btn"]');
  }

  async goto(id?: string) {
    if (id) {
      await this.page.goto(`/admin/articles/edit/${id}`);
    } else {
      await this.page.goto('/admin/articles/create');
    }
  }

  async fillArticle(title: string, body: string, abstract: string) {
    await this.titleInput.fill(title);
    await this.abstractInput.fill(abstract);
    await this.bodyEditor.fill(body);
  }

  async save() {
    const url = this.page.url();
    if (url.includes('/create')) {
      await this.page.locator('[data-testid="create-btn"]').click();
    } else {
      await this.saveDraftButton.click();
    }
  }

  async publish() {
    await this.publishButton.click();
  }

  async unpublish() {
    await this.publishButton.click();
  }

  async delete() {
    await this.deleteButton.click();
  }

  async getStatusText() {
    return this.statusBadge.textContent();
  }

  async getSlugText() {
    return this.slugText.textContent();
  }

  async getValidationErrorTexts(): Promise<string[]> {
    const errors = this.page.locator('.form-error');
    return errors.allTextContents();
  }
}

/** @deprecated Use ArticleEditorPage */
export { ArticleEditorPage as AdminArticleEditorPage };
