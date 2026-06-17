import { FormEvent, useState } from "react";
import { Link, useLocation, useNavigate } from "react-router";
import { useAuth } from "../../auth/AuthContext";
import { Button } from "../../ui/Button";
import { Panel } from "../../ui/Panel";

export function LoginPage() {
  const auth = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function submit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      await auth.login({ email, password });
      navigate((location.state as { from?: string } | null)?.from ?? "/dashboard", { replace: true });
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Sign in failed.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <AuthPanel title="Sign in">
      <form className="space-y-4" onSubmit={submit}>
        <Field
          autoComplete="email"
          label="Email"
          value={email}
          onChange={setEmail}
          type="email"
        />
        <Field
          autoComplete="current-password"
          label="Password"
          value={password}
          onChange={setPassword}
          type="password"
        />
        {error ? <p className="text-sm font-medium text-red-700 dark:text-rose-300">{error}</p> : null}
        <Button type="submit" variant="primary" disabled={isSubmitting}>
          Sign in
        </Button>
      </form>
      <p className="mt-5 text-sm text-slate-600 dark:text-slate-400">
        New to RandWise?{" "}
        <Link className="font-semibold text-emerald-800 underline dark:text-emerald-200" to="/register">
          Create an account
        </Link>
      </p>
    </AuthPanel>
  );
}

export function RegisterPage() {
  const auth = useAuth();
  const navigate = useNavigate();
  const [displayName, setDisplayName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function submit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      await auth.register({ displayName, email, password });
      navigate("/onboarding", { replace: true });
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Registration failed.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <AuthPanel title="Create account">
      <form className="space-y-4" onSubmit={submit}>
        <Field
          autoComplete="name"
          label="Name"
          value={displayName}
          onChange={setDisplayName}
        />
        <Field
          autoComplete="email"
          label="Email"
          value={email}
          onChange={setEmail}
          type="email"
        />
        <Field
          autoComplete="new-password"
          label="Password"
          value={password}
          onChange={setPassword}
          type="password"
        />
        {error ? <p className="text-sm font-medium text-red-700 dark:text-rose-300">{error}</p> : null}
        <Button type="submit" variant="primary" disabled={isSubmitting}>
          Create account
        </Button>
      </form>
      <p className="mt-5 text-sm text-slate-600 dark:text-slate-400">
        Already registered?{" "}
        <Link className="font-semibold text-emerald-800 underline dark:text-emerald-200" to="/login">
          Sign in
        </Link>
      </p>
    </AuthPanel>
  );
}

function AuthPanel({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="mx-auto flex min-h-screen max-w-md items-center px-4">
      <Panel className="w-full">
        <p className="mb-2 text-sm font-semibold text-emerald-800 dark:text-emerald-200">
          RandWise
        </p>
        <h1 className="text-2xl font-bold text-slate-950 dark:text-slate-100">{title}</h1>
        <div className="mt-6">{children}</div>
      </Panel>
    </div>
  );
}

function Field({
  label,
  value,
  onChange,
  type = "text",
  autoComplete
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  type?: string;
  autoComplete?: string;
}) {
  return (
    <label className="block text-sm font-semibold text-slate-800 dark:text-slate-200">
      {label}
      <input
        autoComplete={autoComplete}
        className="mt-2 min-h-11 w-full rounded-md border border-slate-300 bg-white px-3 text-base text-slate-950 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-700 dark:border-slate-600 dark:bg-slate-950 dark:text-slate-100"
        value={value}
        onChange={(event) => onChange(event.target.value)}
        type={type}
        required
      />
    </label>
  );
}
