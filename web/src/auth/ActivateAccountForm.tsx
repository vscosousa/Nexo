import { usePreferences } from "../shared/preferences/Preferences";
import { Notice } from "../shared/Notice";
import { useEffect, useState, type FormEvent } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { Check } from "lucide-react";
import { useAuth } from "./AuthContext";
import { authService, type PendingExternal } from "./authService";
import { CodeInput } from "./CodeInput";
import { Field } from "./Field";
import { GoogleIcon } from "./GoogleIcon";
import { googleSignInUrl } from "./googleSignInUrl";
import { PasswordStrengthMeter } from "./PasswordStrengthMeter";
import { measurePassword } from "./passwordStrength";
import { describeApiError } from "../shared/http/apiError";

const CODE_LENGTH = 6;

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
  const [password, setPassword] = useState("");
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [error, setError] = useState(
    params.get("error") ? m.auth.googleFailed : "",
  );
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
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
    } catch (e) {
      setError(
        describeApiError(e, m, { 403: m.auth.invitationInvalid }).message,
      );
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
    setFieldErrors({});
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
        const failure = describeApiError(e, m, {
          403: m.auth.googleWrongAccount,
          409: m.auth.activateConflict,
          401: m.auth.googleExpired,
        });
        if (failure.status === 401) setPending(null);
        setFieldErrors(failure.fieldErrors);
        setError(failure.message);
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
    if (
      measurePassword(dto.password, dto.firstName, dto.lastName).strength ===
      "weak"
    ) {
      setError(m.auth.weakPassword);
      return;
    }
    setSubmitting(true);
    try {
      await authService.activate(dto);
      navigate("/login?activated=1", { replace: true });
    } catch (e) {
      const failure = describeApiError(e, m, {
        403: m.auth.invitationInvalid,
        409: m.auth.activateConflict,
      });
      if (failure.status === 403 || failure.status === 409) retry();
      setFieldErrors(failure.fieldErrors);
      setError(failure.message);
      setSubmitting(false);
    }
  };

  const onResend = async () => {
    setResending(true);
    setError("");
    try {
      await authService.resendInvitation(email);
      setResent(true);
    } catch (e) {
      setResent(false);
      setError(describeApiError(e, m).message);
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
          {resent && <Notice tone="success">{m.auth.resendSent}</Notice>}
        </>
      )}
      {verified && (
        <>
          <Field
            key={pending ? "google-first" : "first"}
            label={m.auth.firstName}
            name="firstName"
            error={fieldErrors.firstName}
            autoComplete="given-name"
            defaultValue={pending?.firstName ?? undefined}
            onValueChange={setFirstName}
          />
          <Field
            key={pending ? "google-last" : "last"}
            label={m.auth.lastName}
            name="lastName"
            error={fieldErrors.lastName}
            autoComplete="family-name"
            defaultValue={pending?.lastName ?? undefined}
            onValueChange={setLastName}
          />
          {!pending && (
            <>
              <Field
                label={m.auth.password}
                name="password"
                error={fieldErrors.password}
                type="password"
                autoComplete="new-password"
                onValueChange={setPassword}
              />
              <PasswordStrengthMeter
                password={password}
                names={[firstName, lastName]}
              />
            </>
          )}
        </>
      )}
      {error && <Notice tone="error">{error}</Notice>}
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
