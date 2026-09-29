import type { ReactNode } from "react";
import { Check } from "lucide-react";
import { Link } from "react-router-dom";
import { usePreferences } from "../shared/preferences/Preferences";
import "./auth.css";

/** The logo, linking home, the same on every screen size. */
function Logo() {
  return (
    <Link to="/" className="auth-logo" aria-label="Nexo">
      <span className="brand-mark" aria-hidden="true" />
      <span className="brand-word" aria-hidden="true" />
    </Link>
  );
}

/** Legal links pinned to the bottom of the page, as on GitHub's sign-in/register pages. Plain
 * text for now, matching the landing page footer: there are no Terms/Privacy pages yet. */
function SiteFooter() {
  const { m } = usePreferences();
  return (
    <footer className="auth-site-footer">
      <ul>
        {m.common.legalLinks.map((link) => (
          <li key={link}>{link}</li>
        ))}
      </ul>
    </footer>
  );
}

/** Brand pitch beside the form, desktop only (joins at 1024px); the same content as before. */
function BrandPanel() {
  const { m } = usePreferences();
  return (
    <aside className="auth-panel">
      <div className="auth-panel-body">
        <h2>{m.auth.panelTitle}</h2>
        <ul>
          {m.auth.panelPoints.map((point) => (
            <li key={point}>
              <span className="auth-check" aria-hidden="true">
                <Check />
              </span>
              {point}
            </li>
          ))}
        </ul>
      </div>
      <p className="auth-panel-note">{m.auth.panelNote}</p>
    </aside>
  );
}

/**
 * Shared frame for the sign-in, register, and activate screens: the logo and form near the top,
 * no subtitle copy, exactly the same structure in the form column on phones and desktop. Desktop
 * adds a brand panel beside it; the form column itself never changes shape.
 */
export function AuthLayout({
  title,
  children,
  footer,
}: {
  title: string;
  children: ReactNode;
  footer?: ReactNode;
}) {
  return (
    <div className="auth-shell">
      <main className="auth-main">
        <div className="auth-main-content">
          <Logo />
          <div className="auth-card">
            <h1>{title}</h1>
            {children}
          </div>
          {footer && <p className="auth-footer">{footer}</p>}
        </div>
        <SiteFooter />
      </main>
      <BrandPanel />
    </div>
  );
}
