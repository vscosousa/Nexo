import { useId, useState } from "react";
import { Eye, EyeOff } from "lucide-react";
import { usePreferences } from "../shared/preferences/Preferences";

/**
 * Labelled text input shared by the sign-in and register forms. A `password` field gets a
 * show/hide button so people can check what they typed, which matters most on phones.
 */
export function Field({
  label,
  name,
  type = "text",
  autoComplete,
  defaultValue,
}: {
  label: string;
  name: string;
  type?: "text" | "email" | "password";
  autoComplete?: string;
  defaultValue?: string;
}) {
  const id = useId();
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
    </div>
  );
}
