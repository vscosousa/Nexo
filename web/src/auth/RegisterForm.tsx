import { usePreferences } from "../shared/preferences/Preferences";
import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { authService } from "./authService";
import { Field } from "./Field";

const STEPS = ["stepOrg", "stepAdmin"] as const;

/** First message from an ASP.NET validation problem, if the body has one. */
function firstValidationError(data: unknown): string | undefined {
  const errors = (data as { errors?: Record<string, string[]> })?.errors;
  return errors ? Object.values(errors).flat()[0] : undefined;
}

/**
 * Registers an organization and its admin as a two-step wizard: organization details, then the
 * admin's own account. Both steps live in one form (the second stays in the DOM, just hidden) so
 * nothing is lost moving between them, and only the final step submits to the API.
 */
export function RegisterForm() {
  const { m } = usePreferences();
  const navigate = useNavigate();
  const [step, setStep] = useState<1 | 2>(1);
  const [error, setError] = useState("");
  const [submitting, setSubmitting] = useState(false);

  const onSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const get = (name: string) => String(form.get(name) ?? "").trim();

    if (step === 1) {
      if (!get("organizationName")) {
        setError(m.auth.fillEvery);
        return;
      }
      setError("");
      setStep(2);
      return;
    }

    const dto = {
      organizationName: get("organizationName"),
      adminFirstName: get("adminFirstName"),
      adminLastName: get("adminLastName"),
      adminEmail: get("adminEmail"),
      password: String(form.get("password") ?? ""),
    };
    if (Object.values(dto).some((value) => !value)) {
      setError(m.auth.fillEvery);
      return;
    }
    setSubmitting(true);
    setError("");
    try {
      await authService.register(dto);
      navigate("/login?registered=1", { replace: true });
    } catch (e) {
      const response = (e as { response?: { status?: number; data?: unknown } })
        .response;
      const apiMessage =
        response?.status === 400
          ? firstValidationError(response.data)
          : undefined;
      setError(
        response?.status === 409
          ? m.auth.emailTaken
          : (apiMessage ?? m.common.genericError),
      );
      setSubmitting(false);
    }
  };

  const back = () => {
    setError("");
    setStep(1);
  };

  return (
    <form onSubmit={onSubmit} noValidate>
      <ol
        className="wizard-progress"
        aria-label={`${step} / ${STEPS.length}: ${m.auth[STEPS[step - 1]]}`}
      >
        {STEPS.map((key, i) => (
          <li
            key={key}
            className={i + 1 <= step ? "wizard-progress-step-done" : undefined}
          />
        ))}
      </ol>

      <div className="wizard-step-fields" hidden={step !== 1}>
        <Field
          label={m.auth.organizationName}
          name="organizationName"
          autoComplete="organization"
        />
      </div>

      <div className="wizard-step-fields" hidden={step !== 2}>
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
        />
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
  );
}
