import { httpJson } from "@/lib/http";
import type { LoginInput, RegisterInput, AuthResponse } from "../types/auth";

export async function login(input: LoginInput): Promise<AuthResponse> {
  return await httpJson<AuthResponse>("/api/auth/login", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

// Server may return 201 (no token) or 200 with token per plan.
export async function register(input: RegisterInput): Promise<AuthResponse | void> {
  return await httpJson<AuthResponse | void>("/api/auth/register", {
    method: "POST",
    body: JSON.stringify(input),
  });
}
