import { Navigate } from "react-router-dom";
import { useAuth } from "./AuthContext";

/**
 * Landing page for Google sign-in: the API has already set the session cookie, so this only waits
 * for the session check and opens the app, or goes back to sign-in when there is none.
 */
export function LoginCallbackPage() {
  const { status } = useAuth();
  if (status === "loading") return null;
  return <Navigate to={status === "signedIn" ? "/app" : "/login"} replace />;
}
