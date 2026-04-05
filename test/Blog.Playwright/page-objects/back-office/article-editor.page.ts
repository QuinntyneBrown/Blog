import { Page, Locator } from '@playwright/test';

export class AdminArticleEditorPage {
  readonly page: Page;
  readonly titleInput: Locator;
  readonly abstractInput: Locator;
  readonly bodyTextarea: Locator;
  readonly saveDraftButton: Locator;
  readonly publishButton: Locator;
  readonly statusBadge: Locator;

  constructor(page: Page) {
    this.page = page;
    this.titleInput = page.locator('input[name="title"]');
    this.abstractInput = page.locator('textarea[name="abstract"]');
    this.bodyTextarea = page.locator('textarea[name="body"]');
    this.saveDraftButton = page.locator('button[value="save"]');
    this.publishButton = page.locator('button[value="publish"]');
    this.statusBadge = page.locator('.toolbar-left .badge');
  }

  async gotoCreate() {
    await this.page.goto('/admin/articles/create');
  }

  async fillArticle(data: { title: string; abstract: string; body: string }) {
    await this.titleInput.fill(data.title);
    await this.abstractInput.fill(data.abstract);
    await this.bodyTextarea.fill(data.body);
  }

  async saveDraft() {
    await this.saveDraftButton.click();
  }

  async publish() {
    await this.publishButton.click();
  }

  async getStatusText() {
    return this.statusBadge.textContent();
  }
}
