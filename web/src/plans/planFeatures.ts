import type { PlanDto } from "../auth/authService";
import type { Lang, messages } from "../shared/preferences/messages";

type Copy = (typeof messages)["pt"];

export interface PlanFeatureRow {
  key: string;
  label: string;
  included: (plan: PlanDto) => boolean;
}

/** The named features shown per plan card and compared in the table below. Member and resource
 * limits are numeric, not a yes/no row, and are rendered separately from this list. */
export function planFeatureRows(m: Copy): PlanFeatureRow[] {
  return [
    { key: "bookings", label: m.plans.featureBookings, included: () => true },
    {
      key: "incidents",
      label: m.plans.featureIncidents,
      included: (p) => p.hasIncidentTracking,
    },
    {
      key: "expenses",
      label: m.plans.featureExpenses,
      included: (p) => p.hasExpenseTracking,
    },
    {
      key: "history",
      label: m.plans.featureHistory,
      included: (p) => p.hasDecisionHistory,
    },
    { key: "ai", label: m.plans.featureAi, included: (p) => p.hasAiInsights },
    {
      key: "support",
      label: m.plans.featureSupport,
      included: (p) => p.hasPrioritySupport,
    },
  ];
}

/** Formats a plan's monthly price: "Free" at 0, "Contact us" at null, else "€29/month". */
export function formatPrice(price: number | null, lang: Lang, m: Copy) {
  if (price === null) return m.plans.contactUs;
  if (price === 0) return m.plans.priceFree;
  const amount = new Intl.NumberFormat(lang === "pt" ? "pt-PT" : "en-IE", {
    style: "currency",
    currency: "EUR",
    maximumFractionDigits: 0,
  }).format(price);
  return m.plans.pricePerMonth(amount);
}
