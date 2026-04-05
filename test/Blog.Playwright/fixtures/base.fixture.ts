import { test as base } from '@playwright/test';
import { LoginPage } from '../page-objects/back-office/login.page';
import { ArticleListPage } from '../page-objects/back-office/article-list.page';
import { ArticleEditorPage } from '../page-objects/back-office/article-editor.page';
import { DigitalAssetModalPage } from '../page-objects/back-office/digital-asset-modal.page';
import { PublicArticleListPage } from '../page-objects/public/article-list.page';
import { PublicArticleDetailPage } from '../page-objects/public/article-detail.page';
import { NotFoundPage } from '../page-objects/public/not-found.page';
import { testUser } from './test-data';

type BlogFixtures = {
  loginPage: LoginPage;
  articleListPage: ArticleListPage;
  articleEditorPage: ArticleEditorPage;
  digitalAssetModal: DigitalAssetModalPage;
  publicListPage: PublicArticleListPage;
  publicDetailPage: PublicArticleDetailPage;
  notFoundPage: NotFoundPage;
};

async function adminLogin(page: import('@playwright/test').Page) {
  const loginPage = new LoginPage(page);
  await loginPage.goto();
  await loginPage.login(testUser.email, testUser.password);
  await page.waitForURL(/\/admin\/articles/);
}

export const test = base.extend<BlogFixtures>({
  loginPage: async ({ page }, use) => { await use(new LoginPage(page)); },
  articleListPage: async ({ page }, use) => {
    await adminLogin(page);
    await use(new ArticleListPage(page));
  },
  articleEditorPage: async ({ page }, use) => {
    await adminLogin(page);
    await use(new ArticleEditorPage(page));
  },
  digitalAssetModal: async ({ page }, use) => {
    await adminLogin(page);
    await use(new DigitalAssetModalPage(page));
  },
  publicListPage: async ({ page }, use) => { await use(new PublicArticleListPage(page)); },
  publicDetailPage: async ({ page }, use) => { await use(new PublicArticleDetailPage(page)); },
  notFoundPage: async ({ page }, use) => { await use(new NotFoundPage(page)); },
});

export { expect } from '@playwright/test';
