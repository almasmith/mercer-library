import { z } from "zod";

const envSchema = z.object({
  VITE_API_BASE_URL: z.string().url({
    message: "VITE_API_BASE_URL must be a valid URL (e.g., http://localhost:5000)",
  }),
  VITE_DEFAULT_PAGE_SIZE: z.coerce.number().int().positive().default(20),
  VITE_MAX_PAGE_SIZE: z.coerce.number().int().positive().default(100),
});

const parsed = envSchema.safeParse(import.meta.env);

if (!parsed.success) {
  const issues = parsed.error.issues.map(i => `- ${i.path.join(".")}: ${i.message}`).join("\n");
  // Fail fast in development with a readable error; in production, throw too to prevent a broken build.
  throw new Error(
    `Invalid client environment configuration:\n${issues}\n` +
    `Set required VITE_* variables (e.g., in .env) and restart the dev server.`
  );
}

export const env = {
  apiBaseUrl: parsed.data.VITE_API_BASE_URL,
  defaultPageSize: parsed.data.VITE_DEFAULT_PAGE_SIZE,
  maxPageSize: parsed.data.VITE_MAX_PAGE_SIZE,
} as const;

export type ClientEnv = typeof env;
