const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/** Basic shape check; the backend is the source of truth for whether the address is real. */
export const isEmailValid = (email: string) => EMAIL_RE.test(email);

/**
 * Structural password rules mirrored from `api/Services/PasswordPolicy.cs` (length, character
 * classes). The "must not contain your name/organization" rule stays backend-only: it is only
 * ever checked at registration time, alongside the fields it compares against.
 */
export function passwordRequirementChecks(password: string) {
  return {
    length: password.length >= 8 && password.length <= 128,
    upper: /[A-Z]/.test(password),
    lower: /[a-z]/.test(password),
    digit: /[0-9]/.test(password),
    symbol: /[^A-Za-z0-9]/.test(password),
  };
}

export function isPasswordValid(password: string): boolean {
  return Object.values(passwordRequirementChecks(password)).every(Boolean);
}
