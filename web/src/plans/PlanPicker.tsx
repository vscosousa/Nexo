import { Check } from "lucide-react";
import { usePreferences } from "../shared/preferences/Preferences";
import { UNLIMITED, type PlanDto } from "../auth/authService";
import { formatPrice, planFeatureRows } from "./planFeatures";

/** One plan's limits and included named features, formatted as bullets. */
function PlanBullets({ plan }: { plan: PlanDto }) {
  const { m } = usePreferences();
  const members =
    plan.memberLimit >= UNLIMITED
      ? m.plans.membersUnlimited
      : m.plans.members(plan.memberLimit.toLocaleString());
  const resources =
    plan.resourceLimit >= UNLIMITED
      ? m.plans.resourcesUnlimited
      : m.plans.resources(plan.resourceLimit.toLocaleString());
  const features = planFeatureRows(m)
    .filter((row) => row.included(plan))
    .map((row) => row.label);

  return (
    <ul className="plan-features">
      {[members, resources, ...features].map((text) => (
        <li key={text}>
          <Check aria-hidden="true" />
          {text}
        </li>
      ))}
    </ul>
  );
}

/**
 * Plans as a row of cards, each picked and continued from in one action (its own button), the
 * middle tier by member limit called out as recommended once there are at least three plans.
 */
export function PlanPicker({
  plans,
  onPick,
}: {
  plans: PlanDto[];
  onPick: (plan: PlanDto) => void;
}) {
  const { m, lang } = usePreferences();
  const ordered = [...plans].sort((a, b) => a.memberLimit - b.memberLimit);
  const recommendedId =
    ordered.length >= 3 ? ordered[Math.floor(ordered.length / 2)].id : null;

  return (
    <ul className="plan-grid">
      {ordered.map((plan) => (
        <li
          key={plan.id}
          className="plan-card"
          data-recommended={plan.id === recommendedId || undefined}
          data-reveal
        >
          {plan.id === recommendedId && (
            <p className="plan-badge">{m.plans.recommended}</p>
          )}
          <p className="plan-name">{plan.name}</p>
          <p className="plan-price">
            {formatPrice(plan.monthlyPrice, lang, m)}
          </p>
          <PlanBullets plan={plan} />
          <button
            type="button"
            className={
              plan.id === recommendedId
                ? "btn btn-primary btn-lg"
                : "btn btn-secondary btn-lg"
            }
            onClick={() => onPick(plan)}
          >
            {m.plans.continueWith(plan.name)}
          </button>
        </li>
      ))}
    </ul>
  );
}
