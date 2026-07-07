/* ESLint config (ESLint 8, classic .eslintrc format).
 * Non-type-checked ruleset (no parserOptions.project) so linting is fast and does
 * not require the TS build graph. Lints TypeScript/TSX under src/.
 */
module.exports = {
  root: true,
  env: { browser: true, es2022: true, node: true },
  parser: '@typescript-eslint/parser',
  parserOptions: {
    ecmaVersion: 2022,
    sourceType: 'module',
    ecmaFeatures: { jsx: true },
  },
  settings: { react: { version: 'detect' } },
  plugins: ['@typescript-eslint', 'react', 'react-hooks'],
  extends: [
    'eslint:recommended',
    'plugin:@typescript-eslint/recommended',
    'plugin:react/recommended',
    'plugin:react/jsx-runtime',
    'plugin:react-hooks/recommended',
  ],
  ignorePatterns: ['dist', 'node_modules', 'e2e', 'coverage', '*.config.ts', '*.config.js'],
  rules: {
    // New JSX transform — React import not required.
    'react/prop-types': 'off',
    // Apostrophes/quotes in JSX text are fine; this rule is purely cosmetic.
    'react/no-unescaped-entities': 'off',
    // Use the TS-aware unused-vars; allow intentional _-prefixed placeholders.
    'no-unused-vars': 'off',
    '@typescript-eslint/no-unused-vars': [
      'warn',
      { argsIgnorePattern: '^_', varsIgnorePattern: '^_', caughtErrorsIgnorePattern: '^_' },
    ],
    // Downgrade common, non-blocking findings so lint is usable during the sprint.
    '@typescript-eslint/no-explicit-any': 'warn',
    '@typescript-eslint/no-empty-function': 'off',
  },
};
