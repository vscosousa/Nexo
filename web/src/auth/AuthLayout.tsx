import type { ReactNode } from "react";
import { ArrowLeft, Check } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { usePreferences } from "../shared/preferences/Preferences";
import "./auth.css";

/** The logo as part of the artwork: decorative, not a link. */
function Logo() {
  return (
    <span className="brand" role="img" aria-label="Nexo">
      <span className="brand-mark" aria-hidden="true" />
      <span className="brand-word" aria-hidden="true" />
    </span>
  );
}

/**
 * Shared frame for the sign-in and register screens: a brand panel with the product pitch beside the
 * form on desktop, and just the form on phones.
 */
export function AuthLayout({
  title,
  subtitle,
  children,
}: {
  title: string;
  subtitle: string;
  children: ReactNode;
}) {
  const { m } = usePreferences();
  const navigate = useNavigate();
  const back = () => navigate("/");
  return (
    <div className="auth-shell">
      <main className="auth-main">
        <div className="auth-topbar">
          <button type="button" className="back-btn" onClick={back}>
            <ArrowLeft aria-hidden="true" />
            {m.common.back}
          </button>
          <div className="auth-topbar-logo">
            <Logo />
          </div>
        </div>
        <div className="auth-content">
          <header className="auth-heading">
            <h1>{title}</h1>
            <p>{subtitle}</p>
          </header>
          {children}
        </div>
      </main>
      <aside className="auth-panel">
        <div className="auth-panel-brand">
          <Logo />
        </div>
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
    </div>
  );
}
