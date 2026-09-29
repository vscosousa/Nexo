import { usePreferences } from "../shared/preferences/Preferences";
import { useEffect, useState, type FormEvent } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { Check } from "lucide-react";
import { useAuth } from "./AuthContext";
import { authService, type PendingExternal } from "./authService";
import { CodeInput } from "./CodeInput";
import { Field } from "./Field";
import { GoogleIcon } from "./GoogleIcon";
import { googleSignInUrl } from "./googleSignInUrl";
import { isPasswordValid } from "./validation";

const CODE_LENGTH = 6;

/** First message from an ASP.NET validation problem, if the body has one. */
function firstValidationError(data: unknown): string | undefined {
  const errors = (data as { errors?: Record<string, string[]> })?.errors;
  return errors ? Object.values(errors).flat()[0] : undefined;
}

/**
 * Activates an invited account. The email comes from the link (the route requires it), so it is never
 * asked for again. The person still has to type or paste the invitation code, and only once the API
 * confirms it is correct does the code entry give way to the name/password fields: someone who merely
 * opens a forwarded or mistakenly shared link cannot see or fill the account form.
 *
 * Once the code is confirmed, the member can use Google instead of a password. Google sends the
 * browser back here with `external=google`; the form then skips the code, prefills the names from
 * Google (still editable), and activates without a password, signing the member straight in.
 */
export function ActivateAccountForm() {
  const { m } = usePreferences();
  const { login } = useAuth();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const email = params.get("email")!;
  const linkToken = params.get("token")!;
  const external = params.get("external");
  const [code, setCode] = useState("");
  const [verifying, setVerifying] = useState(false);
  const [verified, setVerified] = useState(false);
  const [attempt, setAttempt] = useState(0);
  const [error, setError] = useState(
    params.get("error") ? m.auth.googleFailed : "",
  );
  const [pending, setPending] = useState<PendingExternal | null>(null);
  const [resent, setResent] = useState(false);
  const [resending, setResending] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (!external) return;
    let ignore = false;
    const load = async () => {
      try {
        const details = await authService.pendingExternal();
        if (ignore) return;
        setPending(details);
        setVerified(true);
      } catch {
        if (!ignore) setError(m.auth.googleExpired);
      }
    };
    void load();
    return () => {
      ignore = true;
    };
  }, [external, m]);

  const onCodeChange = async (value: string) => {
    setCode(value);
    if (!value) return;
    setVerifying(true);
    try {
      await authService.verifyInvitation(email, linkToken, value);
      setVerified(true);
      setError("");
    } catch {
      setError(m.auth.invitationInvalid);
    } finally {
      setVerifying(false);
    }
  };

  const retry = () => {
    setVerified(false);
    setCode("");
    setError("");
    setAttempt((n) => n + 1); // remounts CodeInput so its boxes clear
  };

  const onSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const get = (name: string) => String(form.get(name) ?? "").trim();
    if (pending) {
      const names = {
        firstName: get("firstName"),
        lastName: get("lastName"),
      };
      if (!names.firstName || !names.lastName) {
        setError(m.auth.fillEvery);
        return;
      }
      setSubmitting(true);
      try {
        await authService.activateExternal(names);
        login();
        navigate("/app", { replace: true });
      } catch (e) {
        const status = (e as { response?: { status?: number } }).response
          ?.status;
        if (status === 401) setPending(null);
        setError(
          status === 403
            ? m.auth.googleWrongAccount
            : status === 409
              ? m.auth.activateConflict
              : status === 401
                ? m.auth.googleExpired
                : m.common.genericError,
        );
        setSubmitting(false);
      }
      return;
    }
    const dto = {
      email,
      linkToken,
      code,
      firstName: get("firstName"),
      lastName: get("lastName"),
      password: String(form.get("password") ?? ""),
    };
    if (!dto.firstName || !dto.lastName || !dto.password) {
      setError(m.auth.fillEvery);
      return;
    }
    if (!isPasswordValid(dto.password)) {
      setError(m.auth.weakPassword);
      return;
    }
    setSubmitting(true);
    try {
      await authService.activate(dto);
      navigate("/login?activated=1", { replace: true });
    } catch (e) {
      const response = (e as { response?: { status?: number; data?: unknown } })
        .response;
      const apiMessage =
        response?.status === 400
          ? firstValidationError(response.data)
          : undefined;
      if (response?.status === 403 || response?.status === 409) retry();
      setError(
        response?.status === 403
          ? m.auth.invitationInvalid
          : response?.status === 409
            ? m.auth.activateConflict
            : (apiMessage ?? m.common.genericError),
      );
      setSubmitting(false);
    }
  };

  const onResend = async () => {
    setResending(true);
    setError("");
    try {
      await authService.resendInvitation(email);
      setResent(true);
    } catch {
      setResent(false);
      setError(m.common.genericError);
    } finally {
      setResending(false);
    }
  };

  return (
    <form onSubmit={onSubmit} noValidate>
      {pending ? (
        <p className="auth-footer">{m.auth.googleAccount(pending.email)}</p>
      ) : verified ? (
        <p className="invite-verified">
          <Check aria-hidden="true" />
          {m.auth.codeVerified}
        </p>
      ) : (
        <>
          <CodeInput
            key={attempt}
            name="code"
            length={CODE_LENGTH}
            label={m.auth.invitationCode}
            onChange={onCodeChange}
            disabled={verifying}
          />
          <p className="auth-footer">
            {m.auth.resendInvite}{" "}
            <button
              type="button"
              className="resend-invite"
              onClick={onResend}
              disabled={resending}
            >
              {m.auth.resendButton}
            </button>
          </p>
          {resent && (
            <p role="status" className="alert alert-success">
              {m.auth.resendSent}
            </p>
          )}
        </>
      )}
      {verified && (
        <>
          <Field
            key={pending ? "google-first" : "first"}
            label={m.auth.firstName}
            name="firstName"
            autoComplete="given-name"
            defaultValue={pending?.firstName ?? undefined}
          />
          <Field
            key={pending ? "google-last" : "last"}
            label={m.auth.lastName}
            name="lastName"
            autoComplete="family-name"
            defaultValue={pending?.lastName ?? undefined}
          />
          {!pending && (
            <Field
              label={m.auth.password}
              name="password"
              type="password"
              autoComplete="new-password"
            />
          )}
        </>
      )}
      {error && (
        <p role="alert" className="alert alert-error">
          {error}
        </p>
      )}
      {verified && (
        <button
          type="submit"
          className="btn btn-primary btn-lg"
          disabled={submitting}
        >
          {m.auth.activateButton}
        </button>
      )}
      {verified && !pending && (
        <>
          <p className="auth-divider">{m.auth.or}</p>
          <a
            className="btn btn-secondary btn-lg"
            href={googleSignInUrl({
              intent: "activate",
              email,
              token: linkToken,
              code,
            })}
          >
            <GoogleIcon />
            {m.auth.google}
          </a>
        </>
      )}
    </form>
  );
}
