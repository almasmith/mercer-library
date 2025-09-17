import { useSearchParams, Link, useNavigate } from "react-router-dom";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { registerSchema, type RegisterInput, type AuthResponse } from "@/features/auth/types/auth";
import { register as registerApi } from "@/features/auth/api/auth";
import { useAuth } from "@/features/auth/hooks/use-auth";
import { mapProblemDetailsErrors } from "@/features/auth/components/field-errors";
import { HttpError } from "@/lib/http";

export default function RegisterPage() {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const { setAuth } = useAuth();

  const { register, handleSubmit, formState: { errors, isSubmitting }, setError } = useForm<RegisterInput>({
    resolver: zodResolver(registerSchema),
    mode: "onBlur",
  });

  const onSubmit = async (data: RegisterInput) => {
    try {
      const res = (await registerApi(data)) as AuthResponse | void;
      if (res) setAuth(res);
      const returnTo = params.get("returnTo") || "/";
      navigate(returnTo, { replace: true });
    } catch (err: unknown) {
      const pd = err instanceof HttpError ? err.problem : undefined;
      if (pd?.errors) {
        const mapped = mapProblemDetailsErrors(pd.errors);
        Object.entries(mapped).forEach(([field, message]) => setError(field as keyof RegisterInput, { message }));
      } else {
        setError("password", { message: pd?.title || "Registration failed" });
      }
    }
  };

  return (
    <section className="mx-auto max-w-sm">
      <div className="card p-4">
        <h1 className="mb-4 text-xl font-semibold">Create your account</h1>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-3">
          <div>
            <label className="block text-sm">Email</label>
            <input type="email" className="input mt-1" {...register("email")} />
            {errors.email?.message && <p className="mt-1 text-sm text-red-600">{errors.email.message}</p>}
          </div>
          <div>
            <label className="block text-sm">Password</label>
            <input type="password" className="input mt-1" {...register("password")} />
            {errors.password?.message && <p className="mt-1 text-sm text-red-600">{errors.password.message}</p>}
          </div>
          <button disabled={isSubmitting} className="btn-primary">
            {isSubmitting ? "Creating..." : "Create account"}
          </button>
        </form>
        <p className="mt-3 text-sm">
          Already have an account? <Link to="/login" className="text-slate-900 underline">Login</Link>
        </p>
      </div>
    </section>
  );
}


