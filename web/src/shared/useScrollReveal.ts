import { useEffect, type RefObject } from "react";

/**
 * Reveals descendants marked with `data-reveal` as they scroll into view, adding `is-visible`
 * once each intersects and leaving it there. Skips the observer (revealing everything
 * immediately) when the browser lacks IntersectionObserver or the reader prefers reduced motion.
 * Pass `deps` to re-scan for newly rendered `data-reveal` descendants (e.g. after async content loads).
 */
export function useScrollReveal(
  containerRef: RefObject<HTMLElement | null>,
  deps: readonly unknown[] = [],
) {
  useEffect(() => {
    const root = containerRef.current;
    if (!root) return;
    const targets = root.querySelectorAll<HTMLElement>("[data-reveal]");
    if (
      typeof IntersectionObserver === "undefined" ||
      window.matchMedia?.("(prefers-reduced-motion: reduce)").matches
    ) {
      targets.forEach((el) => el.classList.add("is-visible"));
      return;
    }
    const observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          if (entry.isIntersecting) {
            entry.target.classList.add("is-visible");
            observer.unobserve(entry.target);
          }
        }
      },
      { threshold: 0.15 },
    );
    targets.forEach((el) => observer.observe(el));
    return () => observer.disconnect();
  }, [containerRef, ...deps]);
}
