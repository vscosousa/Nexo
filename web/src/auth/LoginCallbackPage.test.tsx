import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AuthProvider } from "./AuthContext";
import { LoginCallbackPage } from "./LoginCallbackPage";
import { authService } from "./authService";

vi.mock("./authService");

function renderCallback() {
  return render(
    <MemoryRouter initialEntries={["/login/callback"]}>
      <AuthProvider>
        <Routes>
          <Route path="/login/callback" element={<LoginCallbackPage />} />
          <Route path="/login" element={<p>Login page</p>} />
          <Route path="/app" element={<p>Home</p>} />
        </Routes>
      </AuthProvider>
    </MemoryRouter>,
  );
}

describe("LoginCallbackPage", () => {
  beforeEach(() => vi.resetAllMocks());

  it("given the API set a session cookie, when the page opens, then it opens the app", async () => {
    vi.mocked(authService.currentAccount).mockResolvedValue({
      id: "a-1",
      organizationId: "o-1",
      role: "Admin",
      email: "ana@example.com",
      firstName: "Ana",
      lastName: "Ribeiro",
    });

    renderCallback();

    expect(await screen.findByText("Home")).toBeInTheDocument();
  });

  it("given no session, when the page opens, then it goes back to the login page", async () => {
    vi.mocked(authService.currentAccount).mockResolvedValue(null);

    renderCallback();

    expect(await screen.findByText("Login page")).toBeInTheDocument();
  });
});
