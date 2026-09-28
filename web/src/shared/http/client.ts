import axios from "axios";

/**
 * Axios instance for the backend API. Attaches the stored auth token to
 * every request and, on a 401 for a request made with a stored token, clears it and redirects to `/login`.
 */
export const apiClient = axios.create({
  baseURL: "/api",
});

apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem("token");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    // Only an expired session redirects; a 401 without one (e.g. a failed sign-in) is the caller's to show.
    if (error.response?.status === 401 && localStorage.getItem("token")) {
      localStorage.removeItem("token");
      window.location.href = "/login";
    }
    return Promise.reject(error);
  },
);
