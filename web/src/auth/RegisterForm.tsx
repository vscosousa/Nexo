import { usePreferences } from "../shared/preferences/Preferences";
import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { authService } from "./authService";
import { Field } from "./Field";

/** First message from an ASP.NET validation problem, if the body has one. */
function firstValidationError(data: unknown): string | undefined {
  const errors = (data as { errors?: Record<string, string[]> })?.errors;
  return errors ? Object.values(errors).flat()[0] : undefined;
}

/** Registers an organization and its admin, then sends them to sign in. */
export function RegisterForm() {
  const { m } = usePreferences();
  const navigate = useNavigate();
  const [error, setError] = useState("");
  const [submitting, setSubmitting] = useState(false);

  const onSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const get = (name: string) => String(form.get(name) ?? "").trim();
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

  return (
    <form onSubmit={onSubmit} noValidate>
      <Field
        label={m.auth.organizationName}
        name="organizationName"
        autoComplete="organization"
      />
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
        {m.auth.createButton}
      </button>
    </form>
  );
}
