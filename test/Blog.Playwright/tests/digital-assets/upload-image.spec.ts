import { test, expect } from '../../fixtures/base.fixture';
import { ToastComponent } from '../../page-objects/back-office/components/toast.component';

import path from 'node:path';

const VALID_JPEG = path.resolve(__dirname, '../../fixtures/assets/sample.jpg');

test.describe('L2-028: Digital Asset Upload – Image Upload', () => {
  let toast: ToastComponent;

  test.beforeEach(async ({ articleEditorPage }) => {
    toast = new ToastComponent(articleEditorPage.page);
    await articleEditorPage.goto();
    await articleEditorPage.featuredImageButton.click();
  });

  test('upload valid JPEG via file input shows preview', async ({ digitalAssetModal }) => {
    await digitalAssetModal.selectFile(VALID_JPEG);

    await expect(digitalAssetModal.preview).toBeVisible();
  });

  test('clicking upload after selecting file shows success toast', async ({ digitalAssetModal }) => {
    await digitalAssetModal.selectFile(VALID_JPEG);
    await digitalAssetModal.upload();

    const message = await toast.waitForSuccess();
    expect(message).toMatch(/upload|success/i);
  });

  test('uploaded image appears in assets list', async ({ digitalAssetModal, authenticatedPage }) => {
    await digitalAssetModal.selectFile(VALID_JPEG);
    await digitalAssetModal.upload();

    await toast.waitForSuccess();

    const assetEntry = authenticatedPage.locator('[data-testid="asset-list"] img[src*="sample"]');
    await expect(assetEntry).toBeVisible();
  });
});
