import { useEffect } from "react";
import { Navigate } from "react-router-dom";
import { useAuth } from "./AuthContext";

/**
 * Landing page for Google sign-in: takes the session token the API put in the URL
 * fragment, stores it, and removes it from the address bar and history straight away.
 */
export function LoginCallbackPage() {
  const { token, login } = useAuth();
  const fragmentToken = new URLSearchParams(window.location.hash.slice(1)).get(
    "token",
  );

  useEffect(() => {
    if (fragmentToken) {
      window.history.replaceState(null, "", window.location.pathname);
      login(fragmentToken);
    }
    // eslint-disable-next-line -- runs once for the token present on arrival
  }, []);

  return <Navigate to={fragmentToken || token ? "/app" : "/login"} replace />;
}
