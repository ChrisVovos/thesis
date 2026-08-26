// @ts-check
const { createCjsPreset } = require('jest-preset-angular/presets');

/**
 * Jest is used instead of the default Karma runner because the suite must run headless in CI without
 * a browser install, and because component tests read better with Angular Testing Library.
 *
 * The configuration is CommonJS rather than TypeScript so that Jest can read it without a TypeScript
 * loader of its own; the tests themselves are still compiled by the Angular preset.
 *
 * @type {import('jest').Config}
 */
module.exports = {
  ...createCjsPreset({
    tsconfig: '<rootDir>/tsconfig.spec.json',
    stringifyContentPathRegex: '\\.(html|svg)$',
  }),
  setupFilesAfterEnv: ['<rootDir>/setup-jest.ts'],
  testEnvironment: 'jsdom',
  testMatch: ['<rootDir>/src/**/*.spec.ts'],
  collectCoverageFrom: [
    'src/app/**/*.ts',
    '!src/app/**/*.spec.ts',
    '!src/app/**/generated/**',
    '!src/app/**/*.routes.ts',
  ],
  coverageDirectory: '<rootDir>/coverage',
  coverageReporters: ['text-summary', 'lcov', 'cobertura'],
};
