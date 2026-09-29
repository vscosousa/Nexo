import { Check, ChevronDown } from "lucide-react";
import { usePreferences } from "../shared/preferences/Preferences";
import { passwordRequirementChecks } from "./validation";

/** Live checklist of the password's structural rules, updating as the person types. Unmet rules
 * stay neutral (a plain dot) rather than red, so the list doesn't read as an error before the
 * person has even started typing. */
export function PasswordRequirements({ password }: { password: string }) {
  const { m } = usePreferences();
  const checks = passwordRequirementChecks(password);
  const items: [boolean, string][] = [
    [checks.length, m.auth.passwordRequirementLength],
    [checks.upper, m.auth.passwordRequirementUpper],
    [checks.lower, m.auth.passwordRequirementLower],
    [checks.digit, m.auth.passwordRequirementDigit],
    [checks.symbol, m.auth.passwordRequirementSymbol],
  ];

  return (
    <details className="password-requirements-details">
      <summary>
        <ChevronDown aria-hidden="true" />
        {m.auth.passwordRequirementsToggle}
      </summary>
      <div className="password-requirements-collapse">
        <ul className="password-requirements">
          {items.map(([met, label]) => (
            <li key={label} data-met={met || undefined}>
              {met ? (
                <Check aria-hidden="true" />
              ) : (
                <span className="password-requirement-dot" aria-hidden="true" />
              )}
              {label}
            </li>
          ))}
        </ul>
      </div>
    </details>
  );
}
