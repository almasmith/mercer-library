import { z } from "zod";

export const loginSchema = z.object({
    email: z.string().email().max(254),
    password: z.string().min(8).max(128),
});

export const registerSchema = z.object({
    email: z.string().email().max(254),
    password: z.string().min(8).max(128),
});

export type LoginInput = z.infer<typeof loginSchema>;
export type RegisterInput = z.infer<typeof registerSchema>;

// Mirrors backend AuthResponse
export type AuthResponse = {
    accessToken: string;
    expiresIn: number;
};


