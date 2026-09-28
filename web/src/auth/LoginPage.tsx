import { usePreferences } from "../shared/preferences/Preferences";
import { Link, useSearchParams } from "react-router-dom";
import { AuthLayout } from "./AuthLayout";
import { SignInForm } from "./SignInForm";

/** Sign-in screen; shows a generic error when returning from a failed Google sign-in. */
export function LoginPage() {
  const { m } = usePreferences();
  const [params] = useSearchParams();
  return (
    <AuthLayout title={m.auth.signInTitle} subtitle={m.auth.signInSubtitle}>
      {(params.get("registered") || params.get("activated")) && (
        <p role="status" className="alert alert-success">
          {params.get("activated") ? m.auth.activated : m.auth.registered}
        </p>
      )}
      <SignInForm
        initialError={params.get("error") ? m.auth.googleFailed : undefined}
      />
      <p className="auth-footer">
        {m.auth.newToNexo} <Link to="/register">{m.auth.createAnAccount}</Link>
      </p>
    </AuthLayout>
  );
}
