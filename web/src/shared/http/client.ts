import axios from "axios";

/**
 * Axios instance for the backend API. The session lives in an httpOnly cookie the browser sends
 * along (`withCredentials`, so it also works when the API is on another origin); changes carry the
 * API's anti-forgery token in `X-XSRF-TOKEN`. `VITE_API_BASE_URL` points it at the API directly
 * instead of the dev server's `/api` proxy.
 */
export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? "/api",
  withCredentials: true,
});

const SAFE_METHODS = ["get", "head", "options"];
let csrfToken: Promise<string> | null = null;
let onUnauthorized = () => {};

/** Forgets the anti-forgery token; call after signing in or out, since the API ties it to the session. */
export function resetCsrfToken() {
  csrfToken = null;
}

/** Sets what runs when any request gets a 401, such as marking the user signed out. */
export function setUnauthorizedHandler(handler: () => void) {
  onUnauthorized = handler;
}

apiClient.interceptors.request.use(async (config) => {
  if (!SAFE_METHODS.includes(config.method ?? "get")) {
    csrfToken ??= apiClient
      .get<{ token: string }>("/auth/csrf")
      .then(({ data }) => data.token);
    try {
      config.headers["X-XSRF-TOKEN"] = await csrfToken;
    } catch (error) {
      csrfToken = null;
      throw error;
    }
  }
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) onUnauthorized();
    return Promise.reject(error);
  },
);
