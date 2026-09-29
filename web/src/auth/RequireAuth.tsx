import type { ReactNode } from "react";
import { Navigate } from "react-router-dom";
import { useAuth } from "./AuthContext";

/**
 * Route guard that redirects to `/login` when there is no session, rendering nothing while that is
 * still being checked.
 */
export function RequireAuth({ children }: { children: ReactNode }) {
  const { status } = useAuth();
  if (status === "loading") return null;
  if (status === "signedOut") {
    return <Navigate to="/login" replace />;
  }
  return children;
}
