import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { useAuth } from "../auth/AuthContext";
import { AppLayout } from "./AppLayout";
import { DashboardPage } from "./DashboardPage";

vi.mock("../auth/AuthContext");

function renderApp(role: string) {
  const logout = vi.fn().mockResolvedValue(undefined);
  vi.mocked(useAuth).mockReturnValue({
    status: "signedIn",
    account: {
      id: "a",
      organizationId: "o",
      role,
      email: "ana@example.com",
      firstName: "Ana",
      lastName: "Ribeiro",
    },
    login: vi.fn(),
    logout,
  });
  render(
    <MemoryRouter initialEntries={["/app"]}>
      <Routes>
        <Route path="/app" element={<AppLayout />}>
          <Route index element={<DashboardPage />} />
          <Route path="resources" element={<p>Resources page</p>} />
        </Route>
        <Route path="/login" element={<p>Login page</p>} />
      </Routes>
    </MemoryRouter>,
  );
  return logout;
}

describe("AppLayout and DashboardPage", () => {
  beforeEach(() => vi.resetAllMocks());

  it("given a signed-in admin, when the app opens, then it shows the dashboard, the built sections and the role", () => {
    renderApp("Admin");

    const nav = screen.getByRole("navigation", { name: "App navigation" });
    expect(
      Array.from(nav.querySelectorAll("a")).map((a) => a.textContent),
    ).toEqual(["Dashboard", "Resources"]);
    expect(screen.getByRole("link", { name: "Dashboard" })).toHaveAttribute(
      "aria-current",
      "page",
    );
    expect(
      screen.getByRole("heading", { level: 1, name: "Dashboard" }),
    ).toBeInTheDocument();
    expect(screen.getByText("Admin")).toBeInTheDocument();
    expect(screen.getByText("Ana Ribeiro")).toBeInTheDocument();
  });

  it("given an admin, when using the dashboard shortcut, then it opens resource registration", () => {
    renderApp("Admin");

    expect(
      screen.getByRole("link", { name: "Register resource" }),
    ).toHaveAttribute("href", "/app/resources?new=1");
  });

  it("given a member, when the dashboard opens, then there is no registration shortcut", () => {
    renderApp("Member");

    expect(
      screen.queryByRole("link", { name: "Register resource" }),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole("link", { name: "View resources" }),
    ).toBeInTheDocument();
  });

  it("given the Resources link, when clicked, then the resources page opens", async () => {
    renderApp("Staff");

    await userEvent
      .setup()
      .click(screen.getByRole("link", { name: "Resources" }));

    expect(screen.getByText("Resources page")).toBeInTheDocument();
  });

  it("given a signed-in account, when signing out, then it ends the session and returns to sign-in", async () => {
    const logout = renderApp("Staff");

    await userEvent
      .setup()
      .click(screen.getByRole("button", { name: "Sign out" }));

    expect(logout).toHaveBeenCalled();
    expect(await screen.findByText("Login page")).toBeInTheDocument();
  });
});
