import { test, expect } from '@playwright/test';
import { LoginPage } from '../../page-objects/back-office/login.page';
import { viewports } from '../../helpers/viewport';
import { testUser } from '../../fixtures/test-data';

/**
 * Login page layout and functionality tests.
 *
 * Responsive rules (Login.cshtml):
 *   > 767px  : .login-left (brand panel) visible, .login-right is 480px wide
 *  ≤ 767px  : .login-left hidden, .login-right is full width
 *
 * Viewport mapping:
 *   XL 1440 → both panels
 *   LG  992 → both panels
 *   MD  768 → both panels  (768 > 767)
 *   SM  576 → form only
 *   XS  375 → form only
 */

// ---------------------------------------------------------------------------
// Layout — two-panel desktop view
// ---------------------------------------------------------------------------

test.describe('Login layout — desktop (brand panel visible)', () => {
  for (const [name, viewport] of [
    ['XL', viewports.xl],
    ['LG', viewports.lg],
    ['MD', viewports.md],
  ] as const) {
    test(`brand panel is visible at ${name} (${viewport.width}px)`, async ({ page }) => {
      const loginPage = new LoginPage(page);
      await page.setViewportSize(viewport);
      await loginPage.goto();

      await expect(loginPage.brandPanel).toBeVisible();
      await expect(loginPage.brandLogo).toBeVisible();
      await expect(loginPage.brandLabel).toBeVisible();
      await expect(loginPage.tagline).toBeVisible();
      await expect(loginPage.copyright).toBeVisible();
    });

    test(`form panel is visible alongside brand panel at ${name}`, async ({ page }) => {
      const loginPage = new LoginPage(page);
      await page.setViewportSize(viewport);
      await loginPage.goto();

      await expect(loginPage.formPanel).toBeVisible();
    });
  }
});

// ---------------------------------------------------------------------------
// Layout — single-panel mobile view
// ---------------------------------------------------------------------------

test.describe('Login layout — mobile (brand panel hidden)', () => {
  for (const [name, viewport] of [
    ['SM', viewports.sm],
    ['XS', viewports.xs],
  ] as const) {
    test(`brand panel is hidden at ${name} (${viewport.width}px)`, async ({ page }) => {
      const loginPage = new LoginPage(page);
      await page.setViewportSize(viewport);
      await loginPage.goto();

      await expect(loginPage.brandPanel).toBeHidden();
    });

    test(`form panel takes full width at ${name}`, async ({ page }) => {
      const loginPage = new LoginPage(page);
      await page.setViewportSize(viewport);
      await loginPage.goto();

      await expect(loginPage.formPanel).toBeVisible();

      const formWidth = await loginPage.formPanel.evaluate((el) => el.getBoundingClientRect().width);
      const bodyWidth = await page.evaluate(() => document.body.getBoundingClientRect().width);
      expect(formWidth).toBeCloseTo(bodyWidth, 0);
    });
  }
});

// ---------------------------------------------------------------------------
// Form content — present at every viewport
// ---------------------------------------------------------------------------

test.describe('Login form — visible at all viewports', () => {
  for (const [name, viewport] of Object.entries(viewports)) {
    test(`form fields and submit button are visible at ${name.toUpperCase()}`, async ({ page }) => {
      const loginPage = new LoginPage(page);
      await page.setViewportSize(viewport);
      await loginPage.goto();

      await expect(loginPage.formTitle).toBeVisible();
      await expect(loginPage.formTitle).toHaveText('Sign in');
      await expect(loginPage.formDesc).toBeVisible();
      await expect(loginPage.emailInput).toBeVisible();
      await expect(loginPage.passwordInput).toBeVisible();
      await expect(loginPage.passwordToggle).toBeVisible();
      await expect(loginPage.submitButton).toBeVisible();
      await expect(loginPage.submitButton).toHaveText('Sign in');
    });
  }
});

// ---------------------------------------------------------------------------
// Password visibility toggle
// ---------------------------------------------------------------------------

test.describe('Password toggle button', () => {
  test('password input starts as type="password"', async ({ page }) => {
    const loginPage = new LoginPage(page);
    await page.setViewportSize(viewports.xl);
    await loginPage.goto();

    expect(await loginPage.getPasswordInputType()).toBe('password');
  });

  test('clicking the toggle reveals the password (type becomes "text")', async ({ page }) => {
    const loginPage = new LoginPage(page);
    await page.setViewportSize(viewports.xl);
    await loginPage.goto();

    await loginPage.passwordInput.fill('MySecretPass1!');
    await loginPage.togglePasswordVisibility();

    expect(await loginPage.getPasswordInputType()).toBe('text');
  });

  test('clicking the toggle a second time hides the password again', async ({ page }) => {
    const loginPage = new LoginPage(page);
    await page.setViewportSize(viewports.xl);
    await loginPage.goto();

    await loginPage.passwordInput.fill('MySecretPass1!');
    await loginPage.togglePasswordVisibility(); // reveal
    await loginPage.togglePasswordVisibility(); // hide

    expect(await loginPage.getPasswordInputType()).toBe('password');
  });

  test('revealed password value is readable', async ({ page }) => {
    const loginPage = new LoginPage(page);
    await page.setViewportSize(viewports.xl);
    await loginPage.goto();

    const secret = 'MySecretPass1!';
    await loginPage.passwordInput.fill(secret);
    await loginPage.togglePasswordVisibility();

    await expect(loginPage.passwordInput).toHaveValue(secret);
    expect(await loginPage.getPasswordInputType()).toBe('text');
  });

  test('toggle button has accessible aria-label', async ({ page }) => {
    const loginPage = new LoginPage(page);
    await page.setViewportSize(viewports.xl);
    await loginPage.goto();

    await expect(loginPage.passwordToggle).toHaveAttribute('aria-label', 'Toggle password');
  });

  // Verify toggle works at every breakpoint
  for (const [name, viewport] of Object.entries(viewports)) {
    test(`password toggle works at ${name.toUpperCase()} (${viewport.width}px)`, async ({ page }) => {
      const loginPage = new LoginPage(page);
      await page.setViewportSize(viewport);
      await loginPage.goto();

      await loginPage.passwordInput.fill('TestPass1!');

      expect(await loginPage.getPasswordInputType()).toBe('password');

      await loginPage.togglePasswordVisibility();
      expect(await loginPage.getPasswordInputType()).toBe('text');

      await loginPage.togglePasswordVisibility();
      expect(await loginPage.getPasswordInputType()).toBe('password');
    });
  }
});

// ---------------------------------------------------------------------------
// Form functionality
// ---------------------------------------------------------------------------

test.describe('Login — valid credentials', () => {
  test('redirects to /admin/articles on success', async ({ page }) => {
    const loginPage = new LoginPage(page);
    await page.setViewportSize(viewports.xl);
    await loginPage.goto();
    await loginPage.login(testUser.email, testUser.password);

    await expect(page).toHaveURL('/admin/articles');
  });

  test('successful login works from mobile viewport', async ({ page }) => {
    const loginPage = new LoginPage(page);
    await page.setViewportSize(viewports.xs);
    await loginPage.goto();
    await loginPage.login(testUser.email, testUser.password);

    await expect(page).toHaveURL('/admin/articles');
  });
});

test.describe('Login — invalid credentials', () => {
  test('shows error message for wrong password', async ({ page }) => {
    const loginPage = new LoginPage(page);
    await page.setViewportSize(viewports.xl);
    await loginPage.goto();
    await loginPage.login(testUser.email, 'wrong-password-xyz');

    await expect(loginPage.errorMessage).toBeVisible();
    await expect(loginPage.errorMessage).toContainText('Invalid');
  });

  test('shows error message for unknown email', async ({ page }) => {
    const loginPage = new LoginPage(page);
    await page.setViewportSize(viewports.xl);
    await loginPage.goto();
    await loginPage.login('nobody@example.invalid', 'irrelevant');

    await expect(loginPage.errorMessage).toBeVisible();
    await expect(loginPage.errorMessage).toContainText('Invalid');
  });

  // Error message must be readable at every viewport
  for (const [name, viewport] of Object.entries(viewports)) {
    test(`error message is visible at ${name.toUpperCase()}`, async ({ page }) => {
      const loginPage = new LoginPage(page);
      await page.setViewportSize(viewport);
      await loginPage.goto();
      await loginPage.login('nobody@example.invalid', 'irrelevant');

      await expect(loginPage.errorMessage).toBeVisible();
      await expect(loginPage.errorMessage).toContainText('Invalid');
    });
  }
});

// ---------------------------------------------------------------------------
// Accessibility basics
// ---------------------------------------------------------------------------

test.describe('Login — accessibility', () => {
  test('email input has associated label', async ({ page }) => {
    const loginPage = new LoginPage(page);
    await page.setViewportSize(viewports.xl);
    await loginPage.goto();

    await expect(page.locator('label[for="email"]')).toBeVisible();
  });

  test('password input has associated label', async ({ page }) => {
    const loginPage = new LoginPage(page);
    await page.setViewportSize(viewports.xl);
    await loginPage.goto();

    await expect(page.locator('label[for="password"]')).toBeVisible();
  });

  test('form can be submitted by pressing Enter in the email field', async ({ page }) => {
    const loginPage = new LoginPage(page);
    await page.setViewportSize(viewports.xl);
    await loginPage.goto();

    await loginPage.emailInput.fill(testUser.email);
    await loginPage.passwordInput.fill(testUser.password);
    await loginPage.emailInput.press('Enter');

    await expect(page).toHaveURL('/admin/articles');
  });

  test('page title identifies the sign-in screen', async ({ page }) => {
    const loginPage = new LoginPage(page);
    await page.setViewportSize(viewports.xl);
    await loginPage.goto();

    await expect(page).toHaveTitle(/Sign In/i);
  });
});
