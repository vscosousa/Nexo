import { LayoutDashboard, LogOut, Warehouse } from "lucide-react";
import { Link, NavLink, Outlet, useNavigate } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { usePreferences } from "../shared/preferences/Preferences";
import "./app.css";

/**
 * Frame for every signed-in screen, after the Figma `nexo-dashboard` frame: a dark sidebar (a top bar on phones)
 * with the sections built so far and the signed-in person (initials, name, role) with sign-out; the current
 * section renders beside it.
 */
export function AppLayout() {
  const { m } = usePreferences();
  const { account, logout } = useAuth();
  const navigate = useNavigate();
  const name =
    [account?.firstName, account?.lastName].filter(Boolean).join(" ") ||
    account?.email;
  const initials = (name ?? "")
    .split(/[\s@.]+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0].toUpperCase())
    .join("");

  const signOut = async () => {
    await logout();
    navigate("/login", { replace: true });
  };

  return (
    <div className="app-shell">
      <aside className="app-sidebar">
        <Link to="/app" className="app-brand" aria-label="Nexo">
          <span className="brand-mark" aria-hidden="true" />
          <span className="app-brand-text" aria-hidden="true">
            <span className="brand-word" />
            <span className="app-tagline">{m.app.tagline}</span>
          </span>
        </Link>
        <nav className="app-nav" aria-label={m.app.navLabel}>
          <NavLink to="/app" end>
            <LayoutDashboard aria-hidden="true" />
            {m.app.dashboard}
          </NavLink>
          <NavLink to="/app/resources">
            <Warehouse aria-hidden="true" />
            {m.app.resources}
          </NavLink>
        </nav>
        <div className="app-account">
          {account && (
            <div className="app-person">
              <span className="app-avatar" aria-hidden="true">
                {initials}
              </span>
              <span className="app-person-text">
                <span className="app-person-name">{name}</span>
                <span className="app-person-role">
                  {m.app.roles[account.role] ?? account.role}
                </span>
              </span>
            </div>
          )}
          <button
            type="button"
            className="icon-btn app-sign-out"
            aria-label={m.app.signOut}
            title={m.app.signOut}
            onClick={signOut}
          >
            <LogOut />
          </button>
        </div>
      </aside>
      <main className="app-main">
        <Outlet />
      </main>
    </div>
  );
}
