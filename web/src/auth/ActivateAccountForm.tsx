import { usePreferences } from "../shared/preferences/Preferences";
import { useState, type FormEvent } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { Check } from "lucide-react";
import { authService } from "./authService";
import { CodeInput } from "./CodeInput";
import { Field } from "./Field";

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
 */
export function ActivateAccountForm() {
  const { m } = usePreferences();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const email = params.get("email")!;
  const linkToken = params.get("token")!;
  const [code, setCode] = useState("");
  const [verifying, setVerifying] = useState(false);
  const [verified, setVerified] = useState(false);
  const [attempt, setAttempt] = useState(0);
  const [error, setError] = useState("");
  const [resent, setResent] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  const onCodeChange = async (value: string) => {
    setCode(value);
    if (!value) return;
    setError("");
    setVerifying(true);
    try {
      await authService.verifyInvitation(email, linkToken, value);
      setVerified(true);
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
    setSubmitting(true);
    setError("");
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
    await authService.resendInvitation(email);
    setResent(true);
  };

  return (
    <form onSubmit={onSubmit} noValidate>
      {verified ? (
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
            <button type="button" className="resend-invite" onClick={onResend}>
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
            label={m.auth.firstName}
            name="firstName"
            autoComplete="given-name"
          />
          <Field
            label={m.auth.lastName}
            name="lastName"
            autoComplete="family-name"
          />
          <Field
            label={m.auth.password}
            name="password"
            type="password"
            autoComplete="new-password"
          />
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
    </form>
  );
}
