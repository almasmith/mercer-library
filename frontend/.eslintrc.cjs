module.exports = {
  root: true,
  env: { browser: true, es2023: true },
  parser: "@typescript-eslint/parser",
  parserOptions: {
    ecmaVersion: "latest",
    sourceType: "module",
    project: false,
  },
  settings: {
    react: { version: "detect" },
    "import/resolver": {
      typescript: { project: "frontend/tsconfig.json" },
    },
  },
  plugins: ["@typescript-eslint", "react", "react-hooks", "import"],
  extends: [
    "eslint:recommended",
    "plugin:@typescript-eslint/recommended",
    "plugin:react/recommended",
    "plugin:react-hooks/recommended",
    "plugin:import/recommended",
    "plugin:import/typescript",
    "prettier",
  ],
  rules: {
    "react/react-in-jsx-scope": "off",
    "import/order": [
      "warn",
      { "newlines-between": "always", alphabetize: { order: "asc", caseInsensitive: true } },
    ],
    "react/prop-types": "off",
  },
  ignorePatterns: ["dist", "node_modules"],
};
