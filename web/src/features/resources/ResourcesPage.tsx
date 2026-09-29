import { Plus, Warehouse } from "lucide-react";
import { useState } from "react";
import { useSearchParams } from "react-router-dom";
import { useAuth } from "../../auth/AuthContext";
import { Notice } from "../../shared/Notice";
import { usePreferences } from "../../shared/preferences/Preferences";
import { RegisterResourceForm } from "./RegisterResourceForm";
import { canManageResources } from "./permissions";

/**
 * The Resources section. Admin and staff register resources from a dialog (opened straight away by
 * `?new=1`, the dashboard shortcut); listing them is a later story, so the page body says so.
 */
export function ResourcesPage() {
  const { m } = usePreferences();
  const { account } = useAuth();
  const canManage = canManageResources(account?.role);
  const [params, setParams] = useSearchParams();
  const [open, setOpen] = useState(canManage && params.has("new"));
  const [registered, setRegistered] = useState<string>();

  const close = () => {
    setOpen(false);
    if (params.has("new")) setParams({}, { replace: true });
  };

  return (
    <>
      <header className="page-header">
        <h1>{m.resources.title}</h1>
        {canManage && (
          <button
            type="button"
            className="btn btn-primary"
            onClick={() => {
              setRegistered(undefined);
              setOpen(true);
            }}
          >
            <Plus aria-hidden="true" />
            {m.resources.register}
          </button>
        )}
      </header>
      <div className="page-body">
        {registered && (
          <Notice tone="success">{m.resources.registered(registered)}</Notice>
        )}
        {!canManage && <p className="page-note">{m.resources.memberNote}</p>}
        <section className="app-card empty-state">
          <Warehouse aria-hidden="true" />
          <h2>{m.resources.emptyTitle}</h2>
          <p>{m.resources.emptyText}</p>
        </section>
      </div>
      {open && (
        <RegisterResourceForm
          onClose={close}
          onRegistered={(resource) => {
            setRegistered(resource.name);
            close();
          }}
        />
      )}
    </>
  );
}
