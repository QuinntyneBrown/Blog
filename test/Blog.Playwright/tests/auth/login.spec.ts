import { test, expect } from '../../fixtures/base.fixture';
import { TEST_ADMIN } from '../../fixtures/test-data';

test.describe('L2-023: JWT Authentication – Login', () => {
  test.beforeEach(async ({ loginPage }) => {
    await loginPage.goto();
  });

  test('valid credentials navigates to /articles', async ({ loginPage, page }) => {
    await loginPage.login(TEST_ADMIN.email, TEST_ADMIN.password);

    await expect(page).toHaveURL(/\/articles$/);
  });

  test('invalid password shows error alert', async ({ loginPage }) => {
    await loginPage.login(TEST_ADMIN.email, 'WrongPassword99!');

    await expect(loginPage.errorMessage).toBeVisible();
    await expect(loginPage.errorMessage).toContainText(/invalid|incorrect|unauthorized/i);
  });

  test('invalid email shows error alert', async ({ loginPage }) => {
    await loginPage.login('nobody@blog.local', TEST_ADMIN.password);

    await expect(loginPage.errorMessage).toBeVisible();
    await expect(loginPage.errorMessage).toContainText(/invalid|incorrect|unauthorized/i);
  });

  test('empty email field shows validation error', async ({ loginPage }) => {
    await loginPage.login('', TEST_ADMIN.password);

    await expect(loginPage.emailInput).toHaveAttribute('validationMessage', /.+/);
  });

  test('empty password field shows validation error', async ({ loginPage }) => {
    await loginPage.login(TEST_ADMIN.email, '');

    await expect(loginPage.passwordInput).toHaveAttribute('validationMessage', /.+/);
  });

  test('login form displays email, password inputs and submit button', async ({ loginPage }) => {
    await expect(loginPage.emailInput).toBeVisible();
    await expect(loginPage.passwordInput).toBeVisible();
    await expect(loginPage.submitButton).toBeVisible();
    await expect(loginPage.submitButton).toBeEnabled();
  });
});
