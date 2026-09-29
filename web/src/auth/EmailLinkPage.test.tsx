import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AuthProvider } from "./AuthContext";
import { ConfirmEmailPage, UnlockAccountPage } from "./EmailLinkPage";
import { LoginPage } from "./LoginPage";
import { authService } from "./authService";

vi.mock("./authService");

function renderAt(url: string) {
  return render(
    <MemoryRouter initialEntries={[url]}>
      <AuthProvider>
        <Routes>
          <Route path="/confirm-email" element={<ConfirmEmailPage />} />
          <Route path="/unlock" element={<UnlockAccountPage />} />
          <Route path="/login" element={<LoginPage />} />
        </Routes>
      </AuthProvider>
    </MemoryRouter>,
  );
}

describe("EmailLinkPage", () => {
  beforeEach(() => vi.resetAllMocks());

  it("given a confirmation link, when it opens, then it confirms the email once and asks the admin to sign in", async () => {
    vi.mocked(authService.confirmEmail).mockResolvedValue(undefined);

    renderAt("/confirm-email?email=ana%40example.com&token=abc123");

    expect(
      await screen.findByText("Email confirmed. You can sign in now."),
    ).toBeInTheDocument();
    expect(authService.confirmEmail).toHaveBeenCalledOnce();
    expect(authService.confirmEmail).toHaveBeenCalledWith(
      "ana@example.com",
      "abc123",
    );
  });

  it("given an expired confirmation link, when it opens, then it explains and offers to register again", async () => {
    vi.mocked(authService.confirmEmail).mockRejectedValue({
      response: { status: 403 },
    });

    renderAt("/confirm-email?email=ana%40example.com&token=old");

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Link invalid or expired. Register again for a new one.",
    );
  });

  it("given an unlock link, when it opens, then it unlocks the account and asks the owner to sign in", async () => {
    vi.mocked(authService.unlockAccount).mockResolvedValue(undefined);

    renderAt("/unlock?email=ana%40example.com&token=abc123");

    expect(
      await screen.findByText("Account unlocked. You can sign in now."),
    ).toBeInTheDocument();
    expect(authService.unlockAccount).toHaveBeenCalledWith(
      "ana@example.com",
      "abc123",
    );
  });

  it("given a stale unlock link, when it opens, then it explains which link to use", async () => {
    vi.mocked(authService.unlockAccount).mockRejectedValue({
      response: { status: 403 },
    });

    renderAt("/unlock?email=ana%40example.com&token=old");

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Link invalid or expired. Use the newest email, or sign in for a new link.",
    );
  });
});
