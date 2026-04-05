import { test, expect } from '../../fixtures/base.fixture';

test.describe('L2-006 AC2/AC3: 404 Not Found Page', () => {
  test('non-existent slug shows 404 page', async ({ publicDetailPage, notFoundPage }) => {
    await publicDetailPage.goto('this-article-does-not-exist');

    const visible = await notFoundPage.isVisible();
    expect(visible).toBe(true);

    await expect(notFoundPage.heading).toBeVisible();
  });

  test('unpublished article slug shows 404 page', async ({ publicDetailPage, notFoundPage }) => {
    await publicDetailPage.goto('unpublished-draft-article');

    const visible = await notFoundPage.isVisible();
    expect(visible).toBe(true);

    await expect(notFoundPage.heading).toBeVisible();
  });

  test('404 page has a link back to articles or home', async ({ publicDetailPage, notFoundPage }) => {
    await publicDetailPage.goto('this-article-does-not-exist');

    await expect(notFoundPage.homeLink).toBeVisible();
    await expect(notFoundPage.homeLink).toHaveAttribute('href', /.+/);

    await notFoundPage.homeLink.click();

    // After clicking, user should be on the articles listing or home page
    await expect(notFoundPage.heading).not.toBeVisible();
  });
});
