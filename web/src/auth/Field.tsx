import { useId, useState } from "react";
import { Eye, EyeOff } from "lucide-react";
import { usePreferences } from "../shared/preferences/Preferences";

/**
 * Labelled text input shared by the sign-in and register forms. A `password` field gets a
 * show/hide button so people can check what they typed, which matters most on phones. An `error`
 * is shown under the input and linked to it (`aria-invalid`, `aria-describedby`) so screen readers
 * announce it with the field.
 */
export function Field({
  label,
  name,
  type = "text",
  autoComplete,
  defaultValue,
  onValueChange,
  error,
}: {
  label: string;
  name: string;
  type?: "text" | "email" | "password";
  autoComplete?: string;
  defaultValue?: string;
  /** Reports each keystroke's value without making the input controlled, for live feedback (e.g. a password checklist). */
  onValueChange?: (value: string) => void;
  error?: string;
}) {
  const id = useId();
  const errorId = `${id}-error`;
  const { m } = usePreferences();
  const [shown, setShown] = useState(false);
  const isPassword = type === "password";
  return (
    <div className="field">
      <label htmlFor={id}>{label}</label>
      <div className="field-control">
        <input
          id={id}
          name={name}
          type={isPassword && shown ? "text" : type}
          autoComplete={autoComplete}
          defaultValue={defaultValue}
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? errorId : undefined}
          onChange={
            onValueChange && ((event) => onValueChange(event.target.value))
          }
        />
        {isPassword && (
          <button
            type="button"
            className="field-toggle"
            aria-label={shown ? m.auth.hidePassword : m.auth.showPassword}
            aria-pressed={shown}
            onClick={() => setShown((value) => !value)}
          >
            {shown ? <EyeOff /> : <Eye />}
          </button>
        )}
      </div>
      {error && (
        <p id={errorId} className="field-error">
          {error}
        </p>
      )}
    </div>
  );
}
