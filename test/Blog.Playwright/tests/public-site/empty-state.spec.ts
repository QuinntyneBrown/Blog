import { test, expect } from '../../fixtures/base.fixture';

test.describe('L2-005 AC3: Empty State When No Published Articles', () => {
  test('when no published articles exist, empty state message is shown', async ({ publicListPage }) => {
    await publicListPage.goto();

    await expect(publicListPage.emptyState).toBeVisible();
  });

  test('empty state contains a relevant message', async ({ publicListPage }) => {
    await publicListPage.goto();

    await expect(publicListPage.emptyState).toBeVisible();

    const emptyStateText = await publicListPage.emptyState.innerText();
    expect(emptyStateText).toMatch(/no articles|nothing|empty|no posts/i);
  });
});
