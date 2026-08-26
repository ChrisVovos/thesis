import { defineConfig, devices } from '@playwright/test';

/**
 * The end-to-end suite runs the same journeys twice, once per API transport.
 *
 * The transport is a project-level parameter rather than a separate copy of each test. If a journey
 * ever needed different steps for REST and for GraphQL, the duplication would show up here
 * immediately — which is exactly the regression the architecture is designed to prevent.
 */
export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  forbidOnly: !!process.env['CI'],
  retries: process.env['CI'] ? 1 : 0,
  workers: 1,
  reporter: process.env['CI'] ? [['github'], ['html', { open: 'never' }]] : [['list']],
  timeout: 60_000,
  expect: { timeout: 10_000 },
  use: {
    baseURL: process.env['E2E_BASE_URL'] ?? 'http://localhost:4200',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'off',
  },
  projects: [
    {
      name: 'rest',
      use: { ...devices['Desktop Chrome'], transport: 'rest' },
    },
    {
      name: 'graphql',
      use: { ...devices['Desktop Chrome'], transport: 'graphql' },
    },
  ],
  webServer: process.env['E2E_BASE_URL']
    ? undefined
    : {
        command: 'npm start',
        url: 'http://localhost:4200',
        reuseExistingServer: !process.env['CI'],
        timeout: 180_000,
      },
});
