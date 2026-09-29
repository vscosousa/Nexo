import { usePreferences } from "../shared/preferences/Preferences";
import { Notice } from "../shared/Notice";
import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "./AuthContext";
import { authService } from "./authService";
import { googleSignInUrl } from "./googleSignInUrl";
import { Field } from "./Field";
import { GoogleIcon } from "./GoogleIcon";
import { isEmailValid } from "./validation";
import { describeApiError } from "../shared/http/apiError";

/**
 * Email/password sign-in, plus the entry point for Google sign-in.
 *
 * @param onAttempt Called on every submit, before validation, so the page can clear notices from earlier steps.
 */
export function SignInForm({
  initialError,
  onAttempt,
}: {
  initialError?: string;
  onAttempt?: () => void;
}) {
  const { m } = usePreferences();
  const { login } = useAuth();
  const navigate = useNavigate();
  const [error, setError] = useState(initialError ?? "");
  const [submitting, setSubmitting] = useState(false);

  const onSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    onAttempt?.();
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
      setError(
        describeApiError(e, m, {
          400: m.auth.badCredentials,
          401: m.auth.badCredentials,
        }).message,
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
        {error && <Notice tone="error">{error}</Notice>}
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
