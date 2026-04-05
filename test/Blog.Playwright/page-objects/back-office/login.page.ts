import { Page, Locator } from '@playwright/test';

export class LoginPage {
  readonly page: Page;

  // Layout panels
  readonly brandPanel: Locator;
  readonly formPanel: Locator;

  // Brand panel content
  readonly brandLogo: Locator;
  readonly brandLabel: Locator;
  readonly tagline: Locator;
  readonly copyright: Locator;

  // Form header
  readonly formTitle: Locator;
  readonly formDesc: Locator;

  // Form fields
  readonly emailInput: Locator;
  readonly passwordInput: Locator;
  readonly passwordToggle: Locator;
  readonly submitButton: Locator;

  // Feedback
  readonly errorMessage: Locator;

  constructor(page: Page) {
    this.page = page;

    this.brandPanel = page.locator('.login-left');
    this.formPanel = page.locator('.login-right');

    this.brandLogo = page.locator('.login-brand-logo');
    this.brandLabel = page.locator('.login-brand-label');
    this.tagline = page.locator('.login-tagline');
    this.copyright = page.locator('.login-copyright');

    this.formTitle = page.locator('.login-form-title');
    this.formDesc = page.locator('.login-form-desc');

    this.emailInput = page.locator('input[name="email"]');
    this.passwordInput = page.locator('input[name="password"]');
    this.passwordToggle = page.locator('button.input-icon');
    this.submitButton = page.locator('button[type="submit"]');

    this.errorMessage = page.locator('.error-msg');
  }

  async goto() {
    await this.page.goto('/admin/login');
  }

  async login(email: string, password: string) {
    await this.emailInput.fill(email);
    await this.passwordInput.fill(password);
    await this.submitButton.click();
  }

  async togglePasswordVisibility() {
    await this.passwordToggle.click();
  }

  /** Returns the current value of the password input's `type` attribute. */
  async getPasswordInputType(): Promise<string> {
    return (await this.passwordInput.getAttribute('type')) ?? '';
  }

  async getErrorMessage() {
    return this.errorMessage.textContent();
  }
}
