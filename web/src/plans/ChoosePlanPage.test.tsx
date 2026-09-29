import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes, useSearchParams } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { authService, UNLIMITED } from "../auth/authService";
import { ChoosePlanPage } from "./ChoosePlanPage";

vi.mock("../auth/authService", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../auth/authService")>()),
  authService: { listPlans: vi.fn() },
}));

/** Stands in for the organization form, just showing the `planId` it was handed. */
function OrganizationFormStub() {
  const [params] = useSearchParams();
  return <p>planId={params.get("planId")}</p>;
}

function renderPage() {
  return render(
    <MemoryRouter initialEntries={["/register"]}>
      <Routes>
        <Route path="/register" element={<ChoosePlanPage />} />
        <Route
          path="/register/organization"
          element={<OrganizationFormStub />}
        />
      </Routes>
    </MemoryRouter>,
  );
}

const plans = [
  {
    id: "free-id",
    name: "Free",
    memberLimit: 20,
    resourceLimit: 10,
    monthlyPrice: 0,
    hasIncidentTracking: false,
    hasExpenseTracking: false,
    hasDecisionHistory: false,
    hasAiInsights: false,
    hasPrioritySupport: false,
  },
  {
    id: "team-id",
    name: "Team",
    memberLimit: 100,
    resourceLimit: 100,
    monthlyPrice: 29,
    hasIncidentTracking: true,
    hasExpenseTracking: true,
    hasDecisionHistory: true,
    hasAiInsights: false,
    hasPrioritySupport: false,
  },
  {
    id: "ent-id",
    name: "Enterprise",
    memberLimit: UNLIMITED,
    resourceLimit: UNLIMITED,
    monthlyPrice: null,
    hasIncidentTracking: true,
    hasExpenseTracking: true,
    hasDecisionHistory: true,
    hasAiInsights: true,
    hasPrioritySupport: true,
  },
];

describe("ChoosePlanPage", () => {
  beforeEach(() => vi.resetAllMocks());

  it("given seeded plans, when the page loads, then each plan's name, price and limits are shown", async () => {
    vi.mocked(authService.listPlans).mockResolvedValue(plans);
    renderPage();

    expect((await screen.findAllByText("Free")).length).toBeGreaterThan(0);
    expect(screen.getAllByText("Team").length).toBeGreaterThan(0);
    expect(screen.getAllByText("Enterprise").length).toBeGreaterThan(0);
    expect(screen.getAllByText("Free").length).toBeGreaterThan(0);
    expect(screen.getAllByText("€29/month").length).toBeGreaterThan(0);
    expect(screen.getAllByText("Contact us").length).toBeGreaterThan(0);
    expect(
      screen.getByText("Up to 20 active member accounts"),
    ).toBeInTheDocument();
    expect(
      screen.getAllByText("Unlimited member accounts").length,
    ).toBeGreaterThan(0);
    expect(
      screen.getAllByText("Unlimited registered resources").length,
    ).toBeGreaterThan(0);
  });

  it("given a plan's included features, when the page loads, then its card lists them", async () => {
    vi.mocked(authService.listPlans).mockResolvedValue(plans);
    renderPage();

    await screen.findAllByText("Team");

    expect(
      screen.getAllByText("Incident & maintenance tracking").length,
    ).toBeGreaterThan(0);
    // "AI-powered insights" is a bullet only on Enterprise's own card, plus the row label in both the
    // desktop table and the mobile stacked comparison (both render regardless of viewport in jsdom).
    expect(screen.getAllByText("AI-powered insights").length).toBe(3);
  });

  it("given the middle plan by member limit, when the page loads, then it is called out as recommended", async () => {
    vi.mocked(authService.listPlans).mockResolvedValue(plans);
    renderPage();

    await screen.findAllByText("Team");

    expect(screen.getByText("Recommended")).toBeInTheDocument();
  });

  it("given the plans, when the page loads, then a full comparison table is shown below the cards", async () => {
    vi.mocked(authService.listPlans).mockResolvedValue(plans);
    renderPage();

    expect(await screen.findByText("Compare plans")).toBeInTheDocument();
    const table = screen.getByRole("table");
    expect(table).toHaveTextContent("Priority support");
    expect(table).toHaveTextContent("Member accounts");
    // Enterprise's unlimited limits show as an infinity symbol in the table, not the card's wording.
    expect(table).toHaveTextContent("∞");
    expect(
      screen.getAllByLabelText("Unlimited member accounts").length,
    ).toBeGreaterThan(0);
  });

  it("given a plan, when continuing with it, then it navigates to the organization form carrying its id", async () => {
    const user = userEvent.setup();
    vi.mocked(authService.listPlans).mockResolvedValue(plans);
    renderPage();

    await user.click(
      await screen.findByRole("button", { name: "Continue with Free" }),
    );

    expect(await screen.findByText("planId=free-id")).toBeInTheDocument();
  });

  it("given no plans are seeded, when the page loads, then it shows an empty message instead of cards", async () => {
    vi.mocked(authService.listPlans).mockResolvedValue([]);
    renderPage();

    expect(
      await screen.findByText("No plans are available right now."),
    ).toBeInTheDocument();
    expect(screen.queryByText(/^Continue with/)).not.toBeInTheDocument();
  });

  it("given the plans fail to load, when the page loads, then it shows an error", async () => {
    vi.mocked(authService.listPlans).mockRejectedValue(new Error("network"));
    renderPage();

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Couldn't load the plans. Try again.",
    );
  });
});
