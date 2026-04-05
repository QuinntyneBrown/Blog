import { test, expect } from '../../fixtures/base.fixture';

import path from 'node:path';

const TEXT_FILE = path.resolve(__dirname, '../../fixtures/assets/document.txt');
const OVERSIZED_FILE = path.resolve(__dirname, '../../fixtures/assets/oversized.jpg');
const WRONG_EXTENSION_FILE = path.resolve(__dirname, '../../fixtures/assets/image-as.txt');

test.describe('L2-028: Digital Asset Upload – Validation', () => {
  test.beforeEach(async ({ articleEditorPage }) => {
    await articleEditorPage.goto();
    await articleEditorPage.featuredImageButton.click();
  });

  test('uploading non-image file shows error message', async ({ digitalAssetModal }) => {
    await digitalAssetModal.selectFile(TEXT_FILE);

    await expect(digitalAssetModal.errorMessage).toBeVisible();
    const errorText = await digitalAssetModal.getErrorText();
    expect(errorText).toMatch(/invalid|unsupported|image/i);
  });

  test('uploading file exceeding 10 MB shows file size error', async ({ digitalAssetModal }) => {
    await digitalAssetModal.selectFile(OVERSIZED_FILE);

    await expect(digitalAssetModal.errorMessage).toBeVisible();
    const errorText = await digitalAssetModal.getErrorText();
    expect(errorText).toMatch(/size|large|10\s?MB/i);
  });

  test('uploading file with wrong extension but image content shows error', async ({ digitalAssetModal }) => {
    await digitalAssetModal.selectFile(WRONG_EXTENSION_FILE);

    await expect(digitalAssetModal.errorMessage).toBeVisible();
    const errorText = await digitalAssetModal.getErrorText();
    expect(errorText).toMatch(/extension|type|invalid/i);
  });
});
