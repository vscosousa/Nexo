import { ArrowRight, Plus, Warehouse } from "lucide-react";
import { Link } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { canManageResources } from "../features/resources/permissions";
import { usePreferences } from "../shared/preferences/Preferences";

/** The app's entry point after signing in: the page's main action in the top bar and a welcome card. */
export function DashboardPage() {
  const { m } = usePreferences();
  const { account } = useAuth();
  const canManage = canManageResources(account?.role);

  return (
    <>
      <header className="page-header">
        <h1>{m.app.dashboard}</h1>
        {canManage && (
          <Link className="btn btn-primary" to="/app/resources?new=1">
            <Plus aria-hidden="true" />
            {m.resources.register}
          </Link>
        )}
      </header>
      <div className="page-body">
        <section className="app-card app-welcome">
          <span className="app-card-icon" aria-hidden="true">
            <Warehouse />
          </span>
          <div>
            <h2>{m.app.welcomeTitle}</h2>
            <p>{canManage ? m.app.welcomeText : m.app.welcomeMemberText}</p>
            <Link className="app-link" to="/app/resources">
              {m.app.goToResources}
              <ArrowRight aria-hidden="true" />
            </Link>
          </div>
        </section>
      </div>
    </>
  );
}
