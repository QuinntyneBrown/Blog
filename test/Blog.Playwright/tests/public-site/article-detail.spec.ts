import { test, expect } from '../../fixtures/base.fixture';

test.describe('L2-006, L2-038: Public Article Detail', () => {
  const publishedSlug = 'hello-world';

  test('published article renders with title, body, featured image, and date', async ({ publicDetailPage }) => {
    await publicDetailPage.goto(publishedSlug);

    await expect(publicDetailPage.title).toBeVisible();
    await expect(publicDetailPage.body).toBeVisible();
    await expect(publicDetailPage.featuredImage).toBeVisible();
    await expect(publicDetailPage.publishDate).toBeVisible();

    const titleText = await publicDetailPage.getTitleText();
    expect(titleText.length).toBeGreaterThan(0);

    const bodyHtml = await publicDetailPage.getBodyHtml();
    expect(bodyHtml.length).toBeGreaterThan(0);

    await expect(publicDetailPage.featuredImage).toHaveAttribute('src', /.+/);
  });

  test('reading time is displayed', async ({ publicDetailPage }) => {
    await publicDetailPage.goto(publishedSlug);

    await expect(publicDetailPage.readingTime).toBeVisible();

    const readingTimeText = await publicDetailPage.getReadingTimeText();
    expect(readingTimeText).toMatch(/\d+\s*min\s*read/i);
  });

  test('article content is wrapped in semantic article element', async ({ publicDetailPage }) => {
    await publicDetailPage.goto(publishedSlug);

    await expect(publicDetailPage.article).toBeVisible();

    // Verify the title and body are inside the <article> element
    await expect(publicDetailPage.article.locator('[data-testid="article-body"]')).toBeVisible();
    await expect(publicDetailPage.article.getByRole('heading', { level: 1 })).toBeVisible();
  });

  test('publication date uses time element with datetime attribute', async ({ publicDetailPage }) => {
    await publicDetailPage.goto(publishedSlug);

    await expect(publicDetailPage.publishDate).toBeVisible();

    // The publishDate locator targets <time> elements; verify the datetime attribute is a valid ISO date
    const datetime = await publicDetailPage.publishDate.getAttribute('datetime');
    expect(datetime).toBeTruthy();
    expect(new Date(datetime!).toString()).not.toBe('Invalid Date');
  });
});
