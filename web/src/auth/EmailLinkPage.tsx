import { useEffect, useRef, useState } from "react";
import { Notice } from "../shared/Notice";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { usePreferences } from "../shared/preferences/Preferences";
import { AuthLayout } from "./AuthLayout";
import { authService } from "./authService";
import { describeApiError } from "../shared/http/apiError";

/**
 * Landing page of a one-click email link (`?email=…&token=…`): sends the token to the API as soon as it opens,
 * then goes to sign-in with `?<done>=1`, or explains why not (an invalid or expired link, a rate limit, no connection). The token is sent only once,
 * even when React runs the effect twice in development.
 */
function EmailLinkPage({
  title,
  submit,
  done,
  invalid,
}: {
  title: string;
  submit: (email: string, token: string) => Promise<void>;
  done: "confirmed" | "unlocked";
  invalid: string;
}) {
  const { m } = usePreferences();
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const [failure, setFailure] = useState("");
  const sent = useRef(false);

  useEffect(() => {
    if (sent.current) return;
    sent.current = true;
    submit(params.get("email") ?? "", params.get("token") ?? "").then(
      () => navigate(`/login?${done}=1`, { replace: true }),
      (e: unknown) =>
        setFailure(describeApiError(e, m, { 403: invalid }).message),
    );
  }, [done, invalid, m, navigate, params, submit]);

  return (
    <AuthLayout title={title}>
      <div className="email-link-result">
        {failure ? (
          <>
            <Notice tone="error">{failure}</Notice>
            <Link to="/login" className="btn btn-primary btn-lg">
              {m.auth.backToSignIn}
            </Link>
          </>
        ) : (
          <p role="status" className="email-link-checking">
            {m.auth.checkingLink}
          </p>
        )}
      </div>
    </AuthLayout>
  );
}

/** Landing page of the email a newly registered admin gets to confirm the address. */
export function ConfirmEmailPage() {
  const { m } = usePreferences();
  return (
    <EmailLinkPage
      title={m.auth.confirmEmailTitle}
      submit={authService.confirmEmail}
      done="confirmed"
      invalid={m.auth.confirmLinkInvalid}
    />
  );
}

/** Landing page of the email sent when too many wrong passwords lock an account. */
export function UnlockAccountPage() {
  const { m } = usePreferences();
  return (
    <EmailLinkPage
      title={m.auth.unlockTitle}
      submit={authService.unlockAccount}
      done="unlocked"
      invalid={m.auth.unlockLinkInvalid}
    />
  );
}
