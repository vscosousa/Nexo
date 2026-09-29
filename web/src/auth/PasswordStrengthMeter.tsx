import { usePreferences } from "../shared/preferences/Preferences";
import {
  improvementFor,
  measurePassword,
  type PasswordStrength,
} from "./passwordStrength";

const LEVEL: Record<PasswordStrength, number> = {
  weak: 1,
  reasonable: 2,
  strong: 3,
  veryStrong: 4,
};

/**
 * Four-step bar rating the password as it is typed (weak, reasonable, strong, very strong), headed by the rating and
 * followed by the next step to take: what makes a weak password acceptable, or what makes an acceptable one stronger
 * (a missing kind of character first, then length). Exposed to assistive technology as a `meter` whose value text
 * is the rating.
 *
 * @param names Names the password must not contain (the person's, the organization's).
 */
export function PasswordStrengthMeter({
  password,
  names,
}: {
  password: string;
  names: (string | undefined)[];
}) {
  const { m } = usePreferences();
  const { strength, reason } = measurePassword(password, ...names);
  const typed = password.length > 0;
  const level = typed ? LEVEL[strength] : 0;
  const label = typed ? m.auth.passwordStrength[strength] : undefined;
  const next =
    typed && !reason && strength !== "veryStrong"
      ? improvementFor(password)
      : undefined;
  const hint = !typed
    ? undefined
    : reason
      ? m.auth.passwordHint[reason]
      : next && "kind" in next
        ? m.auth.passwordHint.add[next.kind]
        : next
          ? m.auth.passwordHint.longer(next.length)
          : undefined;

  return (
    <div
      className="password-strength"
      data-strength={typed ? strength : undefined}
    >
      <div className="password-strength-head" aria-hidden="true">
        <span>{m.auth.passwordStrengthLabel}</span>
        <span className="password-strength-label">{label}</span>
      </div>
      <div
        role="meter"
        aria-label={m.auth.passwordStrengthLabel}
        aria-valuemin={0}
        aria-valuemax={4}
        aria-valuenow={level}
        aria-valuetext={label}
        className="password-strength-bar"
      >
        {[1, 2, 3, 4].map((step) => (
          <span key={step} data-filled={step <= level || undefined} />
        ))}
      </div>
      {hint && (
        <p className="password-strength-hint" aria-live="polite">
          {hint}
        </p>
      )}
    </div>
  );
}
