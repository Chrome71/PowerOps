import { test, expect } from '@playwright/test';

// What the seed apps show. Change these for your own apps.
const canvasGallery = 'Gallery1';
const table = 'pops_sitevisit';
const gridColumn = 'Visit Date';
const formField = 'Visit';

// Read when a test runs, so `--list` and the type check need no environment.
function env(name: string): string {
  const value = process.env[name];
  if (!value) throw new Error(`Set ${name} first (see README.md).`);
  return value;
}

test('canvas app shows its work items gallery', async ({ page }) => {
  await page.goto(env('CANVAS_APP_URL'));
  const app = page.frameLocator('iframe[name="fullscreen-app-host"]');
  // Checks that the app drew its screen, not its data: Test may hold no row. With known data,
  // wait for an item instead, as Microsoft's samples do: [data-control-part="gallery-item"].
  await expect(app.locator(`[data-control-name="${canvasGallery}"]`).first()).toBeVisible();
});

test('model-driven app shows its grid and form', async ({ page }) => {
  const link = new URL(env('MODEL_DRIVEN_APP_URL'));
  const appId = link.searchParams.get('appid');
  if (!appId) throw new Error('MODEL_DRIVEN_APP_URL has no appid parameter (see README.md).');
  const base = `${link.origin}/main.aspx?appid=${appId}&etn=${table}`;

  await page.goto(`${base}&pagetype=entitylist`);
  await expect(page.getByRole('columnheader', { name: gridColumn })).toBeVisible();

  // A new, unsaved record: nothing is written.
  await page.goto(`${base}&pagetype=entityrecord`);
  await expect(page.getByRole('textbox', { name: formField, exact: true })).toBeVisible();
});
