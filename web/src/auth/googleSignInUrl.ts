/** Public API address the browser is sent to for Google sign-in (must match the redirect URI host). */
const API_ORIGIN = import.meta.env.VITE_API_ORIGIN ?? "http://localhost:5122";

/**
 * Builds the address that starts Google sign-in. Without params it signs in; with an `intent` of
 * `register` (plus `planId` and `organizationName`) or `activate` (plus `email`, `token`, `code`), Google
 * comes back to that form with the details pending instead.
 *
 * @returns An absolute URL on the API.
 */
export function googleSignInUrl(params?: Record<string, string>): string {
  const query = params ? `?${new URLSearchParams(params)}` : "";
  return `${API_ORIGIN}/auth/external/google${query}`;
}
