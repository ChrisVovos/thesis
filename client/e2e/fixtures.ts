import { expect, test as base, type Page } from '@playwright/test';

/** The transports the suite is parameterised over. */
export type Transport = 'rest' | 'graphql';

/** Options every test in this suite receives. */
export interface TransportOptions {
  /** The API surface this project exercises. */
  transport: Transport;
}

/**
 * The base test, extended with the transport parameter and a signed-in page.
 *
 * The transport is applied twice on purpose: it is seeded into local storage so the first request of
 * the session already uses it, and it is then asserted through the toolbar control so the test also
 * proves the visible switch agrees with what the application is doing.
 */
export const test = base.extend<TransportOptions & { signedInPage: Page }>({
  transport: ['rest', { option: true }],

  signedInPage: async ({ page, transport, baseURL }, use) => {
    await page.addInitScript((selected) => {
      window.localStorage.setItem('api-transport', selected);
    }, transport);

    await page.goto(`${baseURL}/sign-in`);

    await page.getByTestId('email').fill(credentials.email);
    await page.getByTestId('password').fill(credentials.password);
    await page.getByTestId('sign-in').click();

    await expect(page).toHaveURL(/\/items/);
    await expect(page.getByTestId('transport-selector')).toContainText(
      transport === 'rest' ? 'REST' : 'GraphQL',
    );

    await use(page);
  },
});

/**
 * The credentials the suite signs in with.
 *
 * They are supplied by the environment, never committed: the same rule the server applies to its
 * seeding options applies to the test harness that uses them.
 */
export const credentials = {
  email: process.env['E2E_EMAIL'] ?? 'administrator@itemauthoring.local',
  password: process.env['E2E_PASSWORD'] ?? '',
};

/**
 * Switches the transport through the toolbar control, as a user would.
 *
 * @param page The page under test.
 * @param transport The transport to switch to.
 */
export async function switchTransport(page: Page, transport: Transport): Promise<void> {
  await page.getByTestId('transport-selector').click();
  await page.getByRole('option', { name: transport === 'rest' ? 'REST' : 'GraphQL' }).click();
  await expect(page.getByTestId('transport-status')).toContainText(
    transport === 'rest' ? 'REST' : 'GraphQL',
  );
}

export { expect };
