import { test, expect } from '../../fixtures/base.fixture';

test.describe('L2-005: Public Article Listing', () => {
  test.beforeEach(async ({ publicListPage }) => {
    await publicListPage.goto();
  });

  test('published articles are displayed as cards', async ({ publicListPage }) => {
    const count = await publicListPage.getCardCount();

    expect(count).toBeGreaterThan(0);
  });

  test('articles are ordered by publication date descending', async ({ publicListPage }) => {
    const firstCard = publicListPage.getCard(0);
    const secondCard = publicListPage.getCard(1);

    const firstDateText = await firstCard.date.innerText();
    const secondDateText = await secondCard.date.innerText();

    const firstDate = new Date(firstDateText);
    const secondDate = new Date(secondDateText);

    expect(firstDate.getTime()).toBeGreaterThanOrEqual(secondDate.getTime());
  });

  test('draft articles do not appear on the public listing', async ({ publicListPage }) => {
    const count = await publicListPage.getCardCount();

    for (let i = 0; i < count; i++) {
      const card = publicListPage.getCard(i);
      const title = await card.getTitleText();

      expect(title).not.toMatch(/\[draft\]/i);
    }
  });

  test('each card shows title, abstract, featured image, and publication date', async ({ publicListPage }) => {
    const card = publicListPage.getCard(0);

    await expect(card.title).toBeVisible();
    await expect(card.abstract).toBeVisible();
    await expect(card.image).toBeVisible();
    await expect(card.date).toBeVisible();

    const titleText = await card.getTitleText();
    expect(titleText.length).toBeGreaterThan(0);

    const abstractText = await card.getAbstractText();
    expect(abstractText.length).toBeGreaterThan(0);

    await expect(card.image).toHaveAttribute('src', /.+/);
    await expect(card.date).toHaveAttribute('datetime', /.+/);
  });

  test('pagination navigates to next page without duplicates', async ({ publicListPage, page }) => {
    // Collect all titles from page 1
    const page1Titles: string[] = [];
    const page1Count = await publicListPage.getCardCount();

    for (let i = 0; i < page1Count; i++) {
      const card = publicListPage.getCard(i);
      page1Titles.push(await card.getTitleText());
    }

    // Navigate to page 2
    await publicListPage.pagination.goToNext();
    await expect(page).toHaveURL(/page=2/);

    // Collect all titles from page 2
    const page2Count = await publicListPage.getCardCount();
    expect(page2Count).toBeGreaterThan(0);

    for (let i = 0; i < page2Count; i++) {
      const card = publicListPage.getCard(i);
      const title = await card.getTitleText();

      expect(page1Titles).not.toContain(title);
    }
  });
});
