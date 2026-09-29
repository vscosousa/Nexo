import { Check, Minus } from "lucide-react";
import type { CSSProperties, ReactNode } from "react";
import { usePreferences } from "../shared/preferences/Preferences";
import { UNLIMITED, type PlanDto } from "../auth/authService";
import { formatPrice, planFeatureRows } from "./planFeatures";

/** A numeric limit's value: the number, or an infinity symbol (with a screen-reader label) when unlimited. */
function LimitValue({
  limit,
  unlimitedLabel,
}: {
  limit: number;
  unlimitedLabel: string;
}) {
  return limit >= UNLIMITED ? (
    <span aria-label={unlimitedLabel} className="plan-table-infinity">
      ∞
    </span>
  ) : (
    <span>{limit.toLocaleString()}</span>
  );
}

/** A feature's value: a checkmark when included, a dash otherwise, both with a screen-reader label. */
function IncludedValue({ included }: { included: boolean }) {
  const { m } = usePreferences();
  return included ? (
    <Check aria-label={m.plans.included} className="plan-table-yes" />
  ) : (
    <Minus aria-label={m.plans.notIncluded} className="plan-table-no" />
  );
}

interface Row {
  key: string;
  label: string;
  value: (plan: PlanDto) => ReactNode;
}

/**
 * Full feature-by-feature comparison across every plan, below the cards: a table from 640px up, and a
 * stacked layout on phones (each row's label on its own line above a grid of its values, one per plan
 * column) so every plan stays visible without a sideways scroll, the way claude.com/pricing's comparison
 * table does on mobile.
 */
export function PlanComparisonTable({ plans }: { plans: PlanDto[] }) {
  const { m, lang } = usePreferences();
  const ordered = [...plans].sort((a, b) => a.memberLimit - b.memberLimit);
  const features = planFeatureRows(m);

  const rows: Row[] = [
    {
      key: "members",
      label: m.plans.membersLabel,
      value: (plan) => (
        <LimitValue
          limit={plan.memberLimit}
          unlimitedLabel={m.plans.membersUnlimited}
        />
      ),
    },
    {
      key: "resources",
      label: m.plans.resourcesLabel,
      value: (plan) => (
        <LimitValue
          limit={plan.resourceLimit}
          unlimitedLabel={m.plans.resourcesUnlimited}
        />
      ),
    },
    ...features.map((feature) => ({
      key: feature.key,
      label: feature.label,
      value: (plan: PlanDto) => (
        <IncludedValue included={feature.included(plan)} />
      ),
    })),
  ];

  return (
    <section className="plan-compare" data-reveal>
      <h2>{m.plans.comparisonTitle}</h2>

      <div className="plan-table-wrap">
        <table>
          <thead>
            <tr>
              <th scope="col">{m.plans.tableFeatureHeader}</th>
              {ordered.map((plan) => (
                <th scope="col" key={plan.id}>
                  <span className="plan-table-name">{plan.name}</span>
                  <span className="plan-table-price">
                    {formatPrice(plan.monthlyPrice, lang, m)}
                  </span>
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr key={row.key}>
                <th scope="row">{row.label}</th>
                {ordered.map((plan) => (
                  <td key={plan.id}>{row.value(plan)}</td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div
        className="plan-compare-mobile"
        style={{ "--plan-count": ordered.length } as CSSProperties}
      >
        <div className="pcm-header">
          {ordered.map((plan) => (
            <span key={plan.id} className="plan-table-name">
              {plan.name}
            </span>
          ))}
        </div>
        {rows.map((row) => (
          <div className="pcm-row" key={row.key}>
            <p className="pcm-label">{row.label}</p>
            <div className="pcm-values">
              {ordered.map((plan) => (
                <span key={plan.id} className="pcm-value">
                  {row.value(plan)}
                </span>
              ))}
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}
