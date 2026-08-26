import { expect, test } from './fixtures';

/**
 * The exam assembly journey, executed once per transport.
 */
test.describe('exam builder', () => {
  test('creates an exam, composes it and publishes it', async ({ signedInPage: page }) => {
    await page.getByRole('link', { name: 'Exams' }).click();
    await expect(page).toHaveURL(/\/exams/);

    await page.getByTestId('create-exam').click();
    const title = `End-to-end exam ${Date.now()}`;
    await page.getByTestId('exam-title').fill(title);
    await page.getByTestId('exam-passing-score').fill('50');
    await page.getByTestId('confirm-new-exam').click();

    await expect(page).toHaveURL(/\/exams\/[0-9a-f-]+$/);
    await expect(page.getByRole('heading', { name: title })).toBeVisible();

    await expect(page.getByTestId('composition-violations')).toBeVisible();

    await page.getByTestId('new-section-title').fill('Part A');
    await page.getByTestId('add-section').click();
    await expect(page.getByRole('heading', { name: 'Part A' })).toBeVisible();

    const firstCandidate = page.locator('[data-testid^="add-item-"]').first();
    await firstCandidate.click();

    await expect(page.getByTestId('composition-violations')).toBeHidden();
    await page.getByTestId('publish-exam').click();

    await expect(page.getByText('Published', { exact: true }).first()).toBeVisible();
  });

  test('refuses to publish an exam with an empty section', async ({ signedInPage: page }) => {
    await page.getByRole('link', { name: 'Exams' }).click();
    await page.getByTestId('create-exam').click();
    await page.getByTestId('exam-title').fill(`Empty section ${Date.now()}`);
    await page.getByTestId('confirm-new-exam').click();

    await page.getByTestId('new-section-title').fill('Part A');
    await page.getByTestId('add-section').click();

    await expect(page.getByTestId('composition-violations')).toContainText(
      'Every section must contain at least one item.',
    );
    await expect(page.getByTestId('publish-exam')).toBeDisabled();
  });
});
