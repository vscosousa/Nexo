import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AuthProvider, useAuth } from "./AuthContext";
import { authService } from "./authService";

vi.mock("./authService");

const staff = {
  id: "a-1",
  organizationId: "o-1",
  role: "Staff",
  email: "sam@example.com",
  firstName: "Sam",
  lastName: "Silva",
};

function Probe() {
  const { account, login, logout } = useAuth();
  return (
    <>
      <p>{account?.role ?? "none"}</p>
      <button onClick={login}>Log in</button>
      <button onClick={() => void logout()}>Log out</button>
    </>
  );
}

describe("AuthProvider", () => {
  beforeEach(() => vi.resetAllMocks());

  it("given a session, when the app loads, then the signed-in account is available", async () => {
    vi.mocked(authService.currentAccount).mockResolvedValue(staff);

    render(
      <AuthProvider>
        <Probe />
      </AuthProvider>,
    );

    expect(await screen.findByText("Staff")).toBeInTheDocument();
  });

  it("given no session at load, when signing in, then it reads the new account", async () => {
    vi.mocked(authService.currentAccount).mockResolvedValueOnce(null);
    render(
      <AuthProvider>
        <Probe />
      </AuthProvider>,
    );
    expect(await screen.findByText("none")).toBeInTheDocument();

    vi.mocked(authService.currentAccount).mockResolvedValueOnce(staff);
    await userEvent
      .setup()
      .click(screen.getByRole("button", { name: "Log in" }));

    expect(await screen.findByText("Staff")).toBeInTheDocument();
  });

  it("given a signed-in account, when signing out, then the account is cleared", async () => {
    vi.mocked(authService.currentAccount).mockResolvedValue(staff);
    vi.mocked(authService.signOut).mockResolvedValue(undefined);
    render(
      <AuthProvider>
        <Probe />
      </AuthProvider>,
    );
    await screen.findByText("Staff");

    await userEvent
      .setup()
      .click(screen.getByRole("button", { name: "Log out" }));

    expect(await screen.findByText("none")).toBeInTheDocument();
  });
});
