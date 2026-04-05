import { test as base } from '@playwright/test';
import { LoginPage } from '../page-objects/back-office/login.page';
import { ArticleListPage } from '../page-objects/back-office/article-list.page';
import { ArticleEditorPage } from '../page-objects/back-office/article-editor.page';
import { DigitalAssetModalPage } from '../page-objects/back-office/digital-asset-modal.page';
import { PublicArticleListPage } from '../page-objects/public/article-list.page';
import { PublicArticleDetailPage } from '../page-objects/public/article-detail.page';
import { NotFoundPage } from '../page-objects/public/not-found.page';
import * as path from 'path';

const STORAGE_STATE_PATH = path.join(__dirname, '..', '.auth-state.json');

type BlogFixtures = {
  loginPage: LoginPage;
  articleListPage: ArticleListPage;
  articleEditorPage: ArticleEditorPage;
  digitalAssetModal: DigitalAssetModalPage;
  publicListPage: PublicArticleListPage;
  publicDetailPage: PublicArticleDetailPage;
  notFoundPage: NotFoundPage;
  authenticatedPage: import('@playwright/test').Page;
};

export const test = base.extend<BlogFixtures>({
  // Authenticated page for admin operations — uses saved storage state
  authenticatedPage: async ({ browser, baseURL }, use) => {
    const ctx = await browser.newContext({
      storageState: STORAGE_STATE_PATH,
      baseURL: baseURL ?? undefined,
    });
    const page = await ctx.newPage();
    await use(page);
    await ctx.close();
  },

  loginPage: async ({ page }, use) => { await use(new LoginPage(page)); },

  articleListPage: async ({ authenticatedPage }, use) => {
    await use(new ArticleListPage(authenticatedPage));
  },
  articleEditorPage: async ({ authenticatedPage }, use) => {
    await use(new ArticleEditorPage(authenticatedPage));
  },
  digitalAssetModal: async ({ authenticatedPage }, use) => {
    await use(new DigitalAssetModalPage(authenticatedPage));
  },

  publicListPage: async ({ page }, use) => { await use(new PublicArticleListPage(page)); },
  publicDetailPage: async ({ page }, use) => { await use(new PublicArticleDetailPage(page)); },
  notFoundPage: async ({ page }, use) => { await use(new NotFoundPage(page)); },
});

export { expect } from '@playwright/test';
