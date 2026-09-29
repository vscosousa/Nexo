import { usePreferences } from "../shared/preferences/Preferences";
import { useEffect, useState, type FormEvent } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { useAuth } from "./AuthContext";
import { authService, type PendingExternal } from "./authService";
import { Field } from "./Field";
import { GoogleIcon } from "./GoogleIcon";
import { googleSignInUrl } from "./googleSignInUrl";
import { PasswordRequirements } from "./PasswordRequirements";
import { isEmailValid, isPasswordValid } from "./validation";

const STEPS = ["stepOrg", "stepAdmin"] as const;

/** First message from an ASP.NET validation problem, if the body has one. */
function firstValidationError(data: unknown): string | undefined {
  const errors = (data as { errors?: Record<string, string[]> })?.errors;
  return errors ? Object.values(errors).flat()[0] : undefined;
}

/**
 * Registers an organization and its admin as a two-step wizard: organization details, then the
 * admin's own account. Both steps live in one form (the second stays in the DOM, just hidden) so
 * nothing is lost moving between them, and only the final step submits to the API, together with
 * the `planId` chosen on the previous page and carried in the URL.
 *
 * On step two the admin can use Google instead of a password. Google then sends the browser back
 * here with `external=google`; the form loads the pending Google details, prefills the names (still
 * editable), and registers without a password, signing the admin straight in.
 */
export function RegisterForm() {
  const { m } = usePreferences();
  const { login } = useAuth();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const planId = params.get("planId") ?? "";
  const external = params.get("external");
  const [step, setStep] = useState<1 | 2>(1);
  const [error, setError] = useState(
    params.get("error") ? m.auth.googleFailed : "",
  );
  const [submitting, setSubmitting] = useState(false);
  const [password, setPassword] = useState("");
  const [organizationName, setOrganizationName] = useState("");
  const [pending, setPending] = useState<PendingExternal | null>(null);

  useEffect(() => {
    if (!external) return;
    let ignore = false;
    const load = async () => {
      try {
        const details = await authService.pendingExternal();
        if (ignore) return;
        setPending(details);
        setOrganizationName(details.organizationName ?? "");
        setStep(2);
      } catch {
        if (!ignore) setError(m.auth.googleExpired);
      }
    };
    void load();
    return () => {
      ignore = true;
    };
  }, [external, m]);

  const fail = (e: unknown) => {
    const response = (e as { response?: { status?: number; data?: unknown } })
      .response;
    const apiMessage =
      response?.status === 400
        ? firstValidationError(response.data)
        : undefined;
    if (response?.status === 401) setPending(null);
    setError(
      response?.status === 409
        ? m.auth.emailTaken
        : response?.status === 401
          ? m.auth.googleExpired
          : (apiMessage ?? m.common.genericError),
    );
    setSubmitting(false);
  };

  const onSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const get = (name: string) => String(form.get(name) ?? "").trim();

    if (step === 1) {
      if (!get("organizationName")) {
        setError(m.auth.fillEvery);
        return;
      }
      setOrganizationName(get("organizationName"));
      setError("");
      setStep(2);
      return;
    }

    if (pending) {
      const dto = {
        organizationName: get("organizationName"),
        adminFirstName: get("adminFirstName"),
        adminLastName: get("adminLastName"),
        planId,
      };
      if (Object.values(dto).some((value) => !value)) {
        setError(m.auth.fillEvery);
        return;
      }
      setSubmitting(true);
      try {
        await authService.registerExternal(dto);
        login();
        navigate("/app", { replace: true });
      } catch (e) {
        fail(e);
      }
      return;
    }

    const dto = {
      organizationName: get("organizationName"),
      adminFirstName: get("adminFirstName"),
      adminLastName: get("adminLastName"),
      adminEmail: get("adminEmail"),
      password: String(form.get("password") ?? ""),
      planId,
    };
    if (Object.values(dto).some((value) => !value)) {
      setError(m.auth.fillEvery);
      return;
    }
    if (!isEmailValid(dto.adminEmail)) {
      setError(m.auth.invalidEmail);
      return;
    }
    if (!isPasswordValid(dto.password)) {
      setError(m.auth.weakPassword);
      return;
    }
    setSubmitting(true);
    try {
      await authService.register(dto);
      navigate("/login?registered=1", { replace: true });
    } catch (e) {
      fail(e);
    }
  };

  const back = () => {
    setError("");
    setStep(1);
  };

  return (
    <>
      <form onSubmit={onSubmit} noValidate>
        <ol
          className="wizard-progress"
          aria-label={`${step} / ${STEPS.length}: ${m.auth[STEPS[step - 1]]}`}
        >
          {STEPS.map((key, i) => (
            <li
              key={key}
              className={
                i + 1 <= step ? "wizard-progress-step-done" : undefined
              }
            />
          ))}
        </ol>

        <div className="wizard-steps">
          <div className="wizard-step-fields" hidden={step !== 1}>
            <Field
              key={pending ? "google" : "form"}
              label={m.auth.organizationName}
              name="organizationName"
              autoComplete="organization"
              defaultValue={pending?.organizationName ?? undefined}
            />
          </div>

          <div className="wizard-step-fields" hidden={step !== 2}>
            {pending ? (
              <>
                <p className="auth-footer">
                  {m.auth.googleAccount(pending.email)}
                </p>
                <Field
                  key="google-first"
                  label={m.auth.firstName}
                  name="adminFirstName"
                  autoComplete="given-name"
                  defaultValue={pending.firstName ?? ""}
                />
                <Field
                  key="google-last"
                  label={m.auth.lastName}
                  name="adminLastName"
                  autoComplete="family-name"
                  defaultValue={pending.lastName ?? ""}
                />
              </>
            ) : (
              <>
                <Field
                  label={m.auth.firstName}
                  name="adminFirstName"
                  autoComplete="given-name"
                />
                <Field
                  label={m.auth.lastName}
                  name="adminLastName"
                  autoComplete="family-name"
                />
                <Field
                  label={m.auth.email}
                  name="adminEmail"
                  type="email"
                  autoComplete="username"
                />
                <Field
                  label={m.auth.password}
                  name="password"
                  type="password"
                  autoComplete="new-password"
                  onValueChange={setPassword}
                />
                <PasswordRequirements password={password} />
              </>
            )}
          </div>
        </div>

        {error && (
          <p role="alert" className="alert alert-error">
            {error}
          </p>
        )}

        <div className="wizard-actions">
          {step === 2 && (
            <button
              type="button"
              className="btn btn-secondary"
              aria-label={`${m.common.back}: ${m.auth.stepOrg}`}
              onClick={back}
            >
              {m.common.back}
            </button>
          )}
          <button
            type="submit"
            className="btn btn-primary btn-lg"
            disabled={submitting}
          >
            {step === 1 ? m.auth.continueButton : m.auth.createButton}
          </button>
        </div>
      </form>
      {step === 2 && !pending && (
        <>
          <p className="auth-divider">{m.auth.or}</p>
          <a
            className="btn btn-secondary btn-lg"
            href={googleSignInUrl({
              intent: "register",
              planId,
              organizationName,
            })}
          >
            <GoogleIcon />
            {m.auth.google}
          </a>
        </>
      )}
    </>
  );
}
