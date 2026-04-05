import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: [['html'], ['json', { outputFile: 'test-results/results.json' }]],
  globalSetup: './global-setup.ts',
  globalTeardown: './global-teardown.ts',
  use: {
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  projects: [
    {
      name: 'back-office-desktop',
      use: {
        ...devices['Desktop Chrome'],
        baseURL: process.env.API_BASE_URL || 'http://localhost:5001',
        storageState: '.auth/admin.json',
      },
      testMatch: ['auth/**', 'article-management/**', 'digital-assets/**'],
    },
    {
      name: 'public-desktop',
      use: {
        ...devices['Desktop Chrome'],
        baseURL: process.env.WEB_BASE_URL || 'http://localhost:5000',
      },
      testMatch: ['public-site/**', 'seo/**', 'security/**', 'performance/**', 'accessibility/**', 'observability/**'],
    },
    {
      name: 'public-tablet',
      use: {
        ...devices['iPad (gen 7)'],
        baseURL: process.env.WEB_BASE_URL || 'http://localhost:5000',
      },
      testMatch: ['responsive/**'],
    },
    {
      name: 'public-mobile',
      use: {
        ...devices['iPhone 13'],
        baseURL: process.env.WEB_BASE_URL || 'http://localhost:5000',
      },
      testMatch: ['responsive/**'],
    },
    {
      name: 'back-office-mobile',
      use: {
        ...devices['iPhone 13'],
        baseURL: process.env.API_BASE_URL || 'http://localhost:5001',
        storageState: '.auth/admin.json',
      },
      testMatch: ['responsive/back-office-responsive.spec.ts'],
    },
    {
      name: 'api',
      use: {
        baseURL: process.env.API_BASE_URL || 'http://localhost:5001',
      },
      testMatch: ['api/**'],
    },
  ],
});
