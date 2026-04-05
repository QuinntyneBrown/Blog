import { chromium, FullConfig } from '@playwright/test';

async function globalSetup(config: FullConfig) {
  const { baseURL } = config.projects[0].use;
  const browser = await chromium.launch();
  const page = await browser.newPage();

  // Wait for the application to be ready
  let retries = 10;
  while (retries-- > 0) {
    try {
      await page.goto(`${baseURL}/health`);
      const text = await page.textContent('body');
      if (text?.includes('healthy')) break;
    } catch {
      await new Promise(r => setTimeout(r, 2000));
    }
  }

  await browser.close();
}

export default globalSetup;
