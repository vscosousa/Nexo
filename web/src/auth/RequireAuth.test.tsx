import { render, screen } from "@testing-library/react";
import type { AxiosAdapter } from "axios";
import { MemoryRouter, Routes, Route } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { apiClient } from "../shared/http/client";
import { AuthProvider } from "./AuthContext";
import { RequireAuth } from "./RequireAuth";
import { authService } from "./authService";

vi.mock("./authService");

const originalAdapter = apiClient.defaults.adapter;
const account = {
  id: "a-1",
  organizationId: "o-1",
  role: "Admin",
  email: "ana@example.com",
  firstName: "Ana",
  lastName: "Ribeiro",
};

function renderProtectedRoute() {
  return render(
    <MemoryRouter initialEntries={["/"]}>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<p>Login page</p>} />
          <Route
            path="/"
            element={
              <RequireAuth>
                <p>Protected content</p>
              </RequireAuth>
            }
          />
        </Routes>
      </AuthProvider>
    </MemoryRouter>,
  );
}

describe("RequireAuth", () => {
  beforeEach(() => vi.resetAllMocks());
  afterEach(() => {
    apiClient.defaults.adapter = originalAdapter;
  });

  it("given no session, when rendering a protected route, then it redirects to login", async () => {
    vi.mocked(authService.currentAccount).mockResolvedValue(null);
    renderProtectedRoute();

    expect(await screen.findByText("Login page")).toBeInTheDocument();
    expect(screen.queryByText("Protected content")).not.toBeInTheDocument();
  });

  it("given a session, when rendering a protected route, then it renders the content", async () => {
    vi.mocked(authService.currentAccount).mockResolvedValue(account);
    renderProtectedRoute();

    expect(await screen.findByText("Protected content")).toBeInTheDocument();
  });

  it("given a session, when the API answers 401, then it goes to login without reloading the page", async () => {
    vi.mocked(authService.currentAccount).mockResolvedValue(account);
    renderProtectedRoute();
    await screen.findByText("Protected content");
    const adapter: AxiosAdapter = (config) =>
      Promise.reject({ config, response: { status: 401, data: {} } });
    apiClient.defaults.adapter = adapter;

    await expect(apiClient.get("/anything")).rejects.toBeDefined();

    expect(await screen.findByText("Login page")).toBeInTheDocument();
  });
});
