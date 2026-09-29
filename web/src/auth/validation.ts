const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/** Basic shape check; the backend is the source of truth for whether the address is real. */
export const isEmailValid = (email: string) => EMAIL_RE.test(email);
