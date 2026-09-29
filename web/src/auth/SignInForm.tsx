import { usePreferences } from "../shared/preferences/Preferences";
import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "./AuthContext";
import { authService } from "./authService";
import { googleSignInUrl } from "./googleSignInUrl";
import { Field } from "./Field";
import { GoogleIcon } from "./GoogleIcon";
import { isEmailValid } from "./validation";

/** Email/password sign-in, plus the entry point for Google sign-in. */
export function SignInForm({ initialError }: { initialError?: string }) {
  const { m } = usePreferences();
  const { login } = useAuth();
  const navigate = useNavigate();
  const [error, setError] = useState(initialError ?? "");
  const [submitting, setSubmitting] = useState(false);

  const onSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const email = String(form.get("email") ?? "").trim();
    const password = String(form.get("password") ?? "");
    if (!email || !password) {
      setError(m.auth.enterCredentials);
      return;
    }
    if (!isEmailValid(email)) {
      setError(m.auth.invalidEmail);
      return;
    }
    setSubmitting(true);
    try {
      await authService.signIn(email, password);
      login();
      navigate("/app", { replace: true });
    } catch (e) {
      const status = (e as { response?: { status?: number } }).response?.status;
      setError(
        status === 401 || status === 400
          ? m.auth.badCredentials
          : m.common.genericError,
      );
      setSubmitting(false);
    }
  };

  return (
    <>
      <form onSubmit={onSubmit} noValidate>
        <Field
          label={m.auth.email}
          name="email"
          type="email"
          autoComplete="username"
        />
        <Field
          label={m.auth.password}
          name="password"
          type="password"
          autoComplete="current-password"
        />
        {error && (
          <p role="alert" className="alert alert-error">
            {error}
          </p>
        )}
        <button
          type="submit"
          className="btn btn-primary btn-lg"
          disabled={submitting}
        >
          {m.auth.signInButton}
        </button>
      </form>
      <p className="auth-divider">{m.auth.or}</p>
      <a className="btn btn-secondary btn-lg" href={googleSignInUrl()}>
        <GoogleIcon />
        {m.auth.google}
      </a>
    </>
  );
}
