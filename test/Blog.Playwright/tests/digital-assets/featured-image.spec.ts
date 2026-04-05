import { test, expect } from '../../fixtures/base.fixture';
import { ToastComponent } from '../../page-objects/back-office/components/toast.component';

import path from 'node:path';

const VALID_JPEG = path.resolve(__dirname, '../../fixtures/assets/sample.jpg');

test.describe('Featured Image in Article Editor', () => {
  test.beforeEach(async ({ articleEditorPage }) => {
    await articleEditorPage.goto();
  });

  test('clicking Featured Image button opens digital asset modal', async ({
    articleEditorPage,
    digitalAssetModal,
  }) => {
    await articleEditorPage.featuredImageButton.click();

    await expect(digitalAssetModal.modal).toBeVisible();
  });

  test('selecting an image sets the featured image preview in editor', async ({
    articleEditorPage,
    digitalAssetModal,
    page,
  }) => {
    const toast = new ToastComponent(page);

    await articleEditorPage.featuredImageButton.click();
    await digitalAssetModal.selectFile(VALID_JPEG);
    await digitalAssetModal.upload();
    await toast.waitForSuccess();

    await expect(articleEditorPage.featuredImagePreview).toBeVisible();
    await expect(articleEditorPage.featuredImagePreview).toHaveAttribute('src', /.+/);
  });

  test('clicking Remove Image clears the featured image preview', async ({
    articleEditorPage,
    digitalAssetModal,
    page,
  }) => {
    const toast = new ToastComponent(page);

    // First set a featured image
    await articleEditorPage.featuredImageButton.click();
    await digitalAssetModal.selectFile(VALID_JPEG);
    await digitalAssetModal.upload();
    await toast.waitForSuccess();

    await expect(articleEditorPage.featuredImagePreview).toBeVisible();

    // Now remove it
    await articleEditorPage.removeFeaturedImageButton.click();

    await expect(articleEditorPage.featuredImagePreview).not.toBeVisible();
  });
});
