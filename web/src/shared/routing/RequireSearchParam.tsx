import type { ReactNode } from "react";
import { Navigate, useSearchParams } from "react-router-dom";

/**
 * Route guard that redirects to `/` when the named query param is missing, e.g. a page that only makes
 * sense reached from a link (an email invitation, a reset link) and not by browsing to it directly.
 */
export function RequireSearchParam({
  name,
  children,
}: {
  name: string;
  children: ReactNode;
}) {
  const [params] = useSearchParams();
  if (!params.get(name)) return <Navigate to="/" replace />;
  return children;
}
