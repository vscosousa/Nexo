import { useEffect, useRef, useState } from "react";
import { Notice } from "../shared/Notice";
import { Link, useNavigate } from "react-router-dom";
import { usePreferences } from "../shared/preferences/Preferences";
import { authService, type PlanDto } from "../auth/authService";
import { SiteFooter } from "../shared/SiteFooter";
import { useScrollReveal } from "../shared/useScrollReveal";
import { PlanComparisonTable } from "./PlanComparisonTable";
import { PlanPicker } from "./PlanPicker";
import "./plans.css";

function Brand() {
  return (
    <Link to="/" className="cp-brand" aria-label="Nexo">
      <span className="brand-mark" aria-hidden="true" />
      <span className="brand-word" aria-hidden="true" />
    </Link>
  );
}

/**
 * Entry point of organization registration: lists the available plans (`GET /plans`) and, once one
 * is picked, moves on to the organization/admin form with that plan carried in the URL.
 */
export function ChoosePlanPage() {
  const { m } = usePreferences();
  const navigate = useNavigate();
  const [plans, setPlans] = useState<PlanDto[] | null>(null);
  const [error, setError] = useState(false);
  const root = useRef<HTMLDivElement>(null);
  useScrollReveal(root, [plans]);

  useEffect(() => {
    authService
      .listPlans()
      .then(setPlans)
      .catch(() => setError(true));
  }, []);

  const onPick = (plan: PlanDto) => {
    navigate(`/register/organization?planId=${plan.id}`);
  };

  return (
    <div className="cp-shell" ref={root}>
      <header className="cp-header">
        <Brand />
        <Link to="/login" className="btn btn-secondary">
          {m.common.signIn}
        </Link>
      </header>

      <main className="cp-main">
        <div className="cp-heading" data-reveal>
          <p className="cp-eyebrow">{m.plans.eyebrow}</p>
          <h1>{m.plans.title}</h1>
          <p className="cp-lead">{m.plans.lead}</p>
        </div>

        {error && <Notice tone="error">{m.plans.loadError}</Notice>}
        {plans && plans.length === 0 && <p>{m.plans.empty}</p>}
        {plans && plans.length > 0 && (
          <>
            <PlanPicker plans={plans} onPick={onPick} />
            <PlanComparisonTable plans={plans} />
          </>
        )}
      </main>

      <SiteFooter />
    </div>
  );
}
