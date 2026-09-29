import { usePreferences } from "../shared/preferences/Preferences";
import { Notice } from "../shared/Notice";
import { useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { AuthLayout } from "./AuthLayout";
import { SignInForm } from "./SignInForm";

/**
 * Sign-in screen; shows a generic error when returning from a failed Google sign-in. A success notice from the
 * previous step (registered, confirmed, activated, unlocked) is hidden once the person tries to sign in, so it
 * never sits next to a sign-in error.
 */
export function LoginPage() {
  const { m } = usePreferences();
  const [params] = useSearchParams();
  const [attempted, setAttempted] = useState(false);
  const notice = attempted
    ? null
    : params.get("activated")
      ? m.auth.activated
      : params.get("confirmed")
        ? m.auth.emailConfirmed
        : params.get("unlocked")
          ? m.auth.unlocked
          : params.get("registered")
            ? m.auth.registered
            : null;
  return (
    <AuthLayout
      title={m.auth.signInTitle}
      footer={
        <>
          {m.auth.newToNexo}{" "}
          <Link to="/register">{m.auth.createAnAccount}</Link>
        </>
      }
    >
      {notice && <Notice tone="success">{notice}</Notice>}
      <SignInForm
        onAttempt={() => setAttempted(true)}
        initialError={params.get("error") ? m.auth.googleFailed : undefined}
      />
    </AuthLayout>
  );
}
