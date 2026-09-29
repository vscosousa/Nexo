import { useEffect } from "react";
import { useLocation } from "react-router-dom";

/**
 * Scrolls the window to the top whenever the path changes, so a new page does not open at the previous
 * page's scroll position (the browser keeps it across client-side navigations). Changes to the query
 * string or hash alone do not scroll. Render once inside the router.
 */
export function ScrollToTop() {
  const { pathname } = useLocation();
  useEffect(() => {
    window.scrollTo(0, 0);
  }, [pathname]);
  return null;
}
