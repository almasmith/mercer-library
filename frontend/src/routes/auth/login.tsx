import { useSearchParams, Link, useNavigate } from "react-router-dom";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { loginSchema, type LoginInput } from "@/features/auth/types/auth";
import { login as loginApi } from "@/features/auth/api/auth";
import { useAuth } from "@/features/auth/hooks/use-auth";
import { mapProblemDetailsErrors } from "@/features/auth/components/field-errors";

export default function LoginPage() {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const { setAuth } = useAuth();

  const { register, handleSubmit, formState: { errors, isSubmitting }, setError } = useForm<LoginInput>({
    resolver: zodResolver(loginSchema),
    mode: "onBlur",
  });

  const onSubmit = async (data: LoginInput) => {
    try {
      const res = await loginApi(data);
      setAuth(res);
      const returnTo = params.get("returnTo") || "/";
      navigate(returnTo, { replace: true });
    } catch (err: any) {
      const pd = err?.problem as { errors?: Record<string, string[]>; title?: string };
      if (pd?.errors) {
        const mapped = mapProblemDetailsErrors(pd.errors);
        Object.entries(mapped).forEach(([field, message]) => setError(field as keyof LoginInput, { message }));
      } else {
        setError("password", { message: pd?.title || "Login failed" });
      }
    }
  };

  return (
    <section className="mx-auto max-w-sm">
      <h1 className="mb-4 text-xl font-semibold">Login</h1>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-3">
        <div>
          <label className="block text-sm">Email</label>
          <input type="email" className="mt-1 w-full rounded border px-3 py-2" {...register("email")} />
          {errors.email?.message && <p className="mt-1 text-sm text-red-600">{errors.email.message}</p>}
        </div>
        <div>
          <label className="block text-sm">Password</label>
          <input type="password" className="mt-1 w-full rounded border px-3 py-2" {...register("password")} />
          {errors.password?.message && <p className="mt-1 text-sm text-red-600">{errors.password.message}</p>}
        </div>
        <button disabled={isSubmitting} className="rounded bg-slate-900 px-3 py-2 text-white disabled:opacity-60">
          {isSubmitting ? "Signing in..." : "Sign in"}
        </button>
      </form>
      <p className="mt-3 text-sm">
        No account? <Link to="/register" className="text-slate-900 underline">Register</Link>
      </p>
    </section>
  );
}


