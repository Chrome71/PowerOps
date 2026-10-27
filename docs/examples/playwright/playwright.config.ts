import path from 'node:path';
import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: 'tests',
  timeout: 180_000,
  // Dataverse galleries and grids can take most of a minute to load.
  expect: { timeout: 60_000 },
  use: {
    // Written by `npx playwright open --save-storage` (see README.md). It holds your sign-in: never commit it.
    storageState: path.join(__dirname, '.auth', 'state.json'),
  },
});
