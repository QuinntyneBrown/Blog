import { test as base, BrowserContext } from '@playwright/test';
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
};

export const test = base.extend<BlogFixtures>({
  loginPage: async ({ page }, use) => { await use(new LoginPage(page)); },
  articleListPage: async ({ browser }, use) => {
    const ctx = await browser.newContext({ storageState: STORAGE_STATE_PATH });
    const page = await ctx.newPage();
    await use(new ArticleListPage(page));
    await ctx.close();
  },
  articleEditorPage: async ({ browser }, use) => {
    const ctx = await browser.newContext({ storageState: STORAGE_STATE_PATH });
    const page = await ctx.newPage();
    await use(new ArticleEditorPage(page));
    await ctx.close();
  },
  digitalAssetModal: async ({ browser }, use) => {
    const ctx = await browser.newContext({ storageState: STORAGE_STATE_PATH });
    const page = await ctx.newPage();
    await use(new DigitalAssetModalPage(page));
    await ctx.close();
  },
  publicListPage: async ({ page }, use) => { await use(new PublicArticleListPage(page)); },
  publicDetailPage: async ({ page }, use) => { await use(new PublicArticleDetailPage(page)); },
  notFoundPage: async ({ page }, use) => { await use(new NotFoundPage(page)); },
});

export { expect } from '@playwright/test';
