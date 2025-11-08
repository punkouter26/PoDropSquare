import { defineConfig, devices } from '@playwright/test';

/**
 * PoDropSquare Playwright E2E Test Configuration
 * 
 * Test Projects:
 * - chromium: Desktop browser testing (1920x1080)
 * - mobile-chrome: Mobile testing (iPhone SE viewport)
 * - tablet: Tablet testing (iPad portrait)
 * 
 * See https://playwright.dev/docs/test-configuration
 */
export default defineConfig({
  testDir: './tests',
  
  /* Run tests in files in parallel */
  fullyParallel: true,
  
  /* Fail the build on CI if you accidentally left test.only in the source code */
  forbidOnly: !!process.env.CI,
  
  /* Retry on CI only */
  retries: process.env.CI ? 2 : 0,
  
  /* Opt out of parallel tests on CI */
  workers: process.env.CI ? 1 : undefined,
  
  /* Reporter to use */
  reporter: [
    ['html', { outputFolder: 'playwright-report' }],
    ['json', { outputFile: 'test-results.json' }],
    ['list']
  ],
  
  /* Shared settings for all the projects below */
  use: {
    /* Base URL to use in actions like `await page.goto('/')` */
    baseURL: process.env.BASE_URL || 'http://localhost:5000',
    
    /* Collect trace when retrying the failed test */
    trace: 'on-first-retry',
    
    /* Screenshot on failure */
    screenshot: 'only-on-failure',
    
    /* Video on failure */
    video: 'retain-on-failure',
    
    /* Maximum time each action can take (e.g., click, fill) */
    actionTimeout: 10000,
  },

  /* Test timeout */
  timeout: 30000,

  /* Configure projects for major browsers */
  projects: [
    {
      name: 'chromium',
      use: { 
        ...devices['Desktop Chrome'],
        viewport: { width: 1920, height: 1080 },
      },
    },

    {
      name: 'mobile-chrome',
      use: { 
        ...devices['iPhone SE'],
      },
    },

    {
      name: 'tablet',
      use: { 
        ...devices['iPad (gen 7)'],
      },
    },
  ],

  /* Run local dev server before starting the tests */
  webServer: {
    command: 'dotnet run --project ../../backend/src/Po.PoDropSquare.Api',
    url: 'http://localhost:5000/api/health',
    reuseExistingServer: !process.env.CI,
    timeout: 120 * 1000,
    stdout: 'pipe',
    stderr: 'pipe',
  },
});
