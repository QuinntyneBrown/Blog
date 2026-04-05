import { test, expect } from '@playwright/test';
import { viewports } from '../../helpers/viewport';
import { SearchPage } from '../../page-objects/public/search.page';

/**
 * Search page layout and functionality tests across all breakpoints.
 *
 * Grid column rules (from Search/Index.cshtml):
 *   > 1199px : 3 columns  (XL)
 *  ≤ 1199px : 2 columns  (LG, MD)
 *  ≤  767px : 1 column   (SM, XS)
 */

// ---------------------------------------------------------------------------
// Empty / no-query state
// ---------------------------------------------------------------------------

test.describe('Search page — empty state (no query)', () => {
  for (const [name, viewport] of Object.entries(viewports)) {
    test(`shows empty state prompt at ${name.toUpperCase()}`, async ({ page }) => {
      await page.setViewportSize(viewport);
      const searchPage = new SearchPage(page);
      await searchPage.goto();

      await expect(searchPage.heroSection).toBeVisible();
      await expect(searchPage.heroInput).toBeVisible();
      await expect(searchPage.filtersBar).toBeVisible();
      await expect(searchPage.filterTagAll).toBeVisible();
      await expect(searchPage.emptyState).toBeVisible();
      await expect(searchPage.emptyHeading).toHaveText('Search for articles');
    });
  }
});

// ---------------------------------------------------------------------------
// Layout — hero bar & filters bar visible at every breakpoint
// ---------------------------------------------------------------------------

test.describe('Search page — layout structure', () => {
  for (const [name, viewport] of Object.entries(viewports)) {
    test(`hero bar is visible at ${name.toUpperCase()}`, async ({ page }) => {
      await page.setViewportSize(viewport);
      const searchPage = new SearchPage(page);
      await searchPage.goto();

      await expect(searchPage.heroSection).toBeVisible();
      await expect(searchPage.heroLabel).toBeVisible();
      await expect(searchPage.heroBar).toBeVisible();
      await expect(searchPage.heroInput).toBeVisible();
    });

    test(`filters bar is visible at ${name.toUpperCase()}`, async ({ page }) => {
      await page.setViewportSize(viewport);
      const searchPage = new SearchPage(page);
      await searchPage.goto();

      await expect(searchPage.filtersBar).toBeVisible();
      await expect(searchPage.filterTagAll).toBeVisible();
    });

    test(`header nav is visible at ${name.toUpperCase()} on search page`, async ({ page }) => {
      await page.setViewportSize(viewport);
      const searchPage = new SearchPage(page);
      await searchPage.goto();

      await expect(page.locator('.nav')).toBeVisible();
      await expect(page.locator('.nav-logo')).toBeVisible();
    });
  }
});

// ---------------------------------------------------------------------------
// Grid column layout at each breakpoint
// ---------------------------------------------------------------------------

test.describe('Search results — grid columns', () => {
  test('3-column grid at XL (1440px)', async ({ page }) => {
    await page.setViewportSize(viewports.xl);
    const searchPage = new SearchPage(page);
    await searchPage.goto('a'); // broad query likely to return results

    const cardCount = await searchPage.getResultCardCount();
    if (cardCount >= 3) {
      const cols = await searchPage.getGridColumnCount();
      expect(cols).toBe(3);
    }
  });

  test('2-column grid at LG (992px)', async ({ page }) => {
    await page.setViewportSize(viewports.lg);
    const searchPage = new SearchPage(page);
    await searchPage.goto('a');

    const cardCount = await searchPage.getResultCardCount();
    if (cardCount >= 2) {
      const cols = await searchPage.getGridColumnCount();
      expect(cols).toBe(2);
    }
  });

  test('2-column grid at MD (768px)', async ({ page }) => {
    await page.setViewportSize(viewports.md);
    const searchPage = new SearchPage(page);
    await searchPage.goto('a');

    const cardCount = await searchPage.getResultCardCount();
    if (cardCount >= 2) {
      const cols = await searchPage.getGridColumnCount();
      expect(cols).toBe(2);
    }
  });

  test('1-column grid at SM (576px)', async ({ page }) => {
    await page.setViewportSize(viewports.sm);
    const searchPage = new SearchPage(page);
    await searchPage.goto('a');

    const cardCount = await searchPage.getResultCardCount();
    if (cardCount >= 1) {
      const cols = await searchPage.getGridColumnCount();
      expect(cols).toBe(1);
    }
  });

  test('1-column grid at XS (375px)', async ({ page }) => {
    await page.setViewportSize(viewports.xs);
    const searchPage = new SearchPage(page);
    await searchPage.goto('a');

    const cardCount = await searchPage.getResultCardCount();
    if (cardCount >= 1) {
      const cols = await searchPage.getGridColumnCount();
      expect(cols).toBe(1);
    }
  });
});

// ---------------------------------------------------------------------------
// Search functionality — submitting via hero bar
// ---------------------------------------------------------------------------

test.describe('Search functionality — hero search bar', () => {
  for (const [name, viewport] of Object.entries(viewports)) {
    test(`submitting query navigates to /search?q=... at ${name.toUpperCase()}`, async ({ page }) => {
      await page.setViewportSize(viewport);
      const searchPage = new SearchPage(page);
      await searchPage.goto();

      await searchPage.heroInput.fill('test');
      await searchPage.heroInput.press('Enter');

      await expect(page).toHaveURL(/\/search\?q=test/);
    });
  }
});

test.describe('Search functionality — result cards', () => {
  test('result cards show title, date, reading time and a Read link', async ({ page }) => {
    await page.setViewportSize(viewports.xl);
    const searchPage = new SearchPage(page);
    await searchPage.goto('a');

    const cardCount = await searchPage.getResultCardCount();
    test.skip(cardCount === 0, 'No search results in database — skipping card content checks');

    const firstCard = searchPage.resultCards.first();
    await expect(firstCard.locator('.article-card-title')).not.toBeEmpty();
    await expect(firstCard.locator('.article-card-meta time')).toBeVisible();
    await expect(firstCard.locator('.article-card-link')).toBeVisible();
    await expect(firstCard.locator('.article-card-link')).toHaveText(/Read article/i);
  });

  test('clicking a result card link navigates to the article', async ({ page }) => {
    await page.setViewportSize(viewports.xl);
    const searchPage = new SearchPage(page);
    await searchPage.goto('a');

    const cardCount = await searchPage.getResultCardCount();
    test.skip(cardCount === 0, 'No search results in database — skipping navigation check');

    const href = await searchPage.firstResultLink();
    expect(href).toMatch(/^\/articles\/.+/);

    await searchPage.resultCards.first().locator('.article-card-link').click();
    await expect(page).toHaveURL(/\/articles\/.+/);
  });

  test('result count label is shown when results exist', async ({ page }) => {
    await page.setViewportSize(viewports.xl);
    const searchPage = new SearchPage(page);
    await searchPage.goto('a');

    const cardCount = await searchPage.getResultCardCount();
    test.skip(cardCount === 0, 'No search results in database — skipping result count check');

    await expect(searchPage.resultCount).toBeVisible();
    await expect(searchPage.resultCount).toContainText(/result/i);
  });
});

test.describe('Search functionality — no results', () => {
  test('shows no-results empty state for unmatched query', async ({ page }) => {
    await page.setViewportSize(viewports.xl);
    const searchPage = new SearchPage(page);
    await searchPage.goto('xyzzy_no_match_42');

    await expect(searchPage.emptyState).toBeVisible();
    await expect(searchPage.emptyHeading).toHaveText(/No results found/i);
  });

  for (const [name, viewport] of Object.entries(viewports)) {
    test(`no-results state is correctly laid out at ${name.toUpperCase()}`, async ({ page }) => {
      await page.setViewportSize(viewport);
      const searchPage = new SearchPage(page);
      await searchPage.goto('xyzzy_no_match_42');

      await expect(searchPage.heroSection).toBeVisible();
      await expect(searchPage.heroInput).toHaveValue('xyzzy_no_match_42');
      await expect(searchPage.emptyState).toBeVisible();
    });
  }
});

// ---------------------------------------------------------------------------
// Clear button — only rendered server-side when query is present
// ---------------------------------------------------------------------------

test.describe('Search hero — clear button', () => {
  test('clear button is present when query is supplied', async ({ page }) => {
    await page.setViewportSize(viewports.xl);
    const searchPage = new SearchPage(page);
    await searchPage.goto('test');

    await expect(searchPage.heroClearButton).toBeVisible();
  });

  test('clear button submits an empty query and resets the page', async ({ page }) => {
    await page.setViewportSize(viewports.xl);
    const searchPage = new SearchPage(page);
    await searchPage.goto('test');

    await searchPage.heroClearButton.click();
    await expect(page).toHaveURL(/\/search(\?q=)?$/);
    await expect(searchPage.emptyState).toBeVisible();
  });
});

// ---------------------------------------------------------------------------
// Navigating to search from the header
// ---------------------------------------------------------------------------

test.describe('Header → Search navigation', () => {
  test('submitting the nav search form at XL goes to /search', async ({ page }) => {
    await page.setViewportSize(viewports.xl);
    await page.goto('/');

    await page.locator('.search-input').fill('test');
    await page.locator('.search-input').press('Enter');

    await expect(page).toHaveURL(/\/search\?q=test/);
    await expect(page.locator('.search-hero')).toBeVisible();
  });

  test('expanding search toggle at MD and submitting goes to /search', async ({ page }) => {
    await page.setViewportSize(viewports.md);
    await page.goto('/');

    await page.locator('.search-toggle').click();
    await page.locator('.search-input').fill('test');
    await page.locator('.search-input').press('Enter');

    await expect(page).toHaveURL(/\/search\?q=test/);
    await expect(page.locator('.search-hero')).toBeVisible();
  });

  test('expanding search toggle at SM and submitting goes to /search', async ({ page }) => {
    await page.setViewportSize(viewports.sm);
    await page.goto('/');

    await page.locator('.search-toggle').click();
    await page.locator('.search-input').fill('test');
    await page.locator('.search-input').press('Enter');

    await expect(page).toHaveURL(/\/search\?q=test/);
    await expect(page.locator('.search-hero')).toBeVisible();
  });

  test('expanding search toggle at XS and submitting goes to /search', async ({ page }) => {
    await page.setViewportSize(viewports.xs);
    await page.goto('/');

    await page.locator('.search-toggle').click();
    await page.locator('.search-input').fill('test');
    await page.locator('.search-input').press('Enter');

    await expect(page).toHaveURL(/\/search\?q=test/);
    await expect(page.locator('.search-hero')).toBeVisible();
  });
});
