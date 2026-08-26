import { expect, switchTransport, test } from './fixtures';

/**
 * The item authoring journey, executed once per transport.
 */
test.describe('item bank', () => {
  test('lists items and filters them', async ({ signedInPage: page }) => {
    await expect(page.getByTestId('item-table')).toBeVisible();

    const before = await page.getByTestId('item-table').locator('tbody tr').count();
    expect(before).toBeGreaterThan(0);

    await page.getByTestId('filter-status').click();
    await page.getByRole('option', { name: 'Published' }).click();
    await page.keyboard.press('Escape');

    await expect(page.getByTestId('item-table')).toBeVisible();
  });

  test('opens the preview of an item', async ({ signedInPage: page }) => {
    await page.getByTestId('item-table').locator('tbody tr a').first().click();

    await expect(page).toHaveURL(/\/items\/[0-9a-f-]+$/);
    await expect(page.getByRole('heading', { name: 'Item preview' })).toBeVisible();
  });

  test('reloads the same screen over the other transport without a manual refresh', async ({
    signedInPage: page,
    transport,
  }) => {
    await expect(page.getByTestId('item-table')).toBeVisible();
    const rowsBefore = await page.getByTestId('item-table').locator('tbody tr').count();

    await switchTransport(page, transport === 'rest' ? 'graphql' : 'rest');

    await expect(page.getByTestId('item-table')).toBeVisible();
    await expect
      .poll(async () => page.getByTestId('item-table').locator('tbody tr').count())
      .toBe(rowsBefore);
  });

  test('rejects an item whose option set breaks the answer shape rules', async ({
    signedInPage: page,
  }) => {
    await page.getByTestId('create-item').click();
    await expect(page).toHaveURL(/\/items\/new/);

    await page.getByTestId('item-stem').fill('End-to-end: an item with no correct option.');
    await page.getByTestId('option-text-0').fill('First');
    await page.getByTestId('option-text-1').fill('Second');
    await page.getByTestId('option-correct-0').locator('input').uncheck({ force: true });

    await page.getByTestId('save-item').click();

    await expect(page.getByTestId('item-form-error')).toContainText('Exactly one');
  });
});
