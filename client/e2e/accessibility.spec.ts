import AxeBuilder from '@axe-core/playwright';
import { expect, test } from './fixtures';

/**
 * Accessibility checks on the screens a user spends most of their time in.
 *
 * The assertion is deliberately narrow — serious and critical violations only — so the suite stays a
 * gate against real regressions rather than a source of noise.
 */
test.describe('accessibility', () => {
  const seriousOrCritical = ['serious', 'critical'];

  test('the item bank has no serious accessibility violations', async ({ signedInPage: page }) => {
    await expect(page.getByTestId('item-table')).toBeVisible();

    const results = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa'])
      .analyze();

    expect(
      results.violations.filter((violation) => seriousOrCritical.includes(violation.impact ?? '')),
    ).toEqual([]);
  });

  test('the sign-in screen has no serious accessibility violations', async ({ page, baseURL }) => {
    await page.goto(`${baseURL}/sign-in`);

    const results = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa'])
      .analyze();

    expect(
      results.violations.filter((violation) => seriousOrCritical.includes(violation.impact ?? '')),
    ).toEqual([]);
  });

  test('the transport selector is reachable and announced', async ({ signedInPage: page }) => {
    const selector = page.getByTestId('transport-selector');

    await expect(selector).toHaveAttribute('aria-label', 'API transport');
    await expect(page.getByTestId('transport-status')).toHaveAttribute('aria-live', 'polite');
  });
});
