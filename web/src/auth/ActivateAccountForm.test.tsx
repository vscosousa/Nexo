import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AuthProvider } from "./AuthContext";
import { ActivateAccountPage } from "./ActivateAccountPage";
import { LoginPage } from "./LoginPage";
import { authService } from "./authService";

vi.mock("./authService");

function renderActivate(url: string) {
  return render(
    <MemoryRouter initialEntries={[url]}>
      <AuthProvider>
        <Routes>
          <Route path="/activate" element={<ActivateAccountPage />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/app" element={<p>Home</p>} />
        </Routes>
      </AuthProvider>
    </MemoryRouter>,
  );
}

async function enterCode(code: string) {
  const user = userEvent.setup();
  for (let i = 0; i < code.length; i++) {
    await user.type(
      screen.getByLabelText(`Invitation code ${i + 1}/6`),
      code[i],
    );
  }
}

async function submit() {
  const user = userEvent.setup();
  await user.type(screen.getByLabelText("First name"), "Rui");
  await user.type(screen.getByLabelText("Last name"), "Costa");
  await user.type(screen.getByLabelText("Password"), "Secret-123");
  await user.click(screen.getByRole("button", { name: "Create account" }));
}

describe("ActivateAccountForm", () => {
  beforeEach(() => vi.resetAllMocks());

  it("given the account form, when it opens, then there is no email field to fill in (it comes from the link)", () => {
    vi.mocked(authService.verifyInvitation).mockResolvedValue(undefined);
    renderActivate("/activate?email=rui%40example.com&token=abc123");

    expect(screen.queryByLabelText("Email")).not.toBeInTheDocument();
  });

  it("given too many code checks, when a code is entered, then it says to wait rather than that the invitation is invalid", async () => {
    vi.mocked(authService.verifyInvitation).mockRejectedValue({
      response: { status: 429, headers: { "retry-after": "20" } },
    });
    renderActivate("/activate?email=rui%40example.com&token=abc123");

    await enterCode("ABC123");

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Too many attempts. Wait up to 20 s.",
    );
  });

  it("given a valid code, when fully entered, then it is verified with the API and gives way to the account fields", async () => {
    vi.mocked(authService.verifyInvitation).mockResolvedValue(undefined);
    renderActivate("/activate?email=rui%40example.com&token=abc123");

    expect(screen.queryByLabelText("First name")).not.toBeInTheDocument();

    await enterCode("ABC123");

    expect(authService.verifyInvitation).toHaveBeenCalledWith(
      "rui@example.com",
      "abc123",
      "ABC123",
    );
    expect(await screen.findByLabelText("First name")).toBeInTheDocument();
    expect(
      screen.queryByLabelText("Invitation code 1/6"),
    ).not.toBeInTheDocument();
  });

  it("given an invalid code, when fully entered, then it says the invitation is not valid and keeps the code entry", async () => {
    vi.mocked(authService.verifyInvitation).mockRejectedValue({
      response: { status: 403 },
    });
    renderActivate("/activate?email=rui%40example.com&token=abc123");

    await enterCode("WRONG1");

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Invalid invitation",
    );
    expect(screen.queryByLabelText("First name")).not.toBeInTheDocument();
  });

  it("given the code and details, when submitting, then it activates and sends the member to sign in", async () => {
    vi.mocked(authService.verifyInvitation).mockResolvedValue(undefined);
    vi.mocked(authService.activate).mockResolvedValue(undefined);
    renderActivate("/activate?email=rui%40example.com&token=abc123");

    await enterCode("ABC123");
    await screen.findByLabelText("First name");
    await submit();

    expect(authService.activate).toHaveBeenCalledWith({
      email: "rui@example.com",
      linkToken: "abc123",
      code: "ABC123",
      firstName: "Rui",
      lastName: "Costa",
      password: "Secret-123",
    });
    expect(await screen.findByRole("status")).toHaveTextContent(
      "Account activated",
    );
  });

  it("given a weak password, when submitting, then it asks for a stronger one without calling the API", async () => {
    vi.mocked(authService.verifyInvitation).mockResolvedValue(undefined);
    renderActivate("/activate?email=rui%40example.com&token=abc123");

    await enterCode("ABC123");
    await screen.findByLabelText("First name");
    const user = userEvent.setup();
    await user.type(screen.getByLabelText("First name"), "Rui");
    await user.type(screen.getByLabelText("Last name"), "Costa");
    await user.type(screen.getByLabelText("Password"), "weakpass");
    await user.click(screen.getByRole("button", { name: "Create account" }));

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Password too weak",
    );
    expect(authService.activate).not.toHaveBeenCalled();
  });

  it("given the resend button, when clicked, then it asks the API to resend and shows a confirmation", async () => {
    vi.mocked(authService.verifyInvitation).mockResolvedValue(undefined);
    vi.mocked(authService.resendInvitation).mockResolvedValue(undefined);
    renderActivate("/activate?email=rui%40example.com&token=abc123");

    await userEvent.click(
      screen.getByRole("button", { name: "Resend invitation" }),
    );

    expect(authService.resendInvitation).toHaveBeenCalledWith(
      "rui@example.com",
    );
    expect(await screen.findByText(/we sent a new code/)).toBeInTheDocument();
  });

  it("given the resend request fails, when clicked, then it shows an error instead of a confirmation", async () => {
    vi.mocked(authService.resendInvitation).mockRejectedValue(
      new Error("down"),
    );
    renderActivate("/activate?email=rui%40example.com&token=abc123");

    await userEvent.click(
      screen.getByRole("button", { name: "Resend invitation" }),
    );

    expect(await screen.findByRole("alert")).toBeInTheDocument();
    expect(screen.queryByText(/we sent a new code/)).toBeNull();
  });

  it("given a verified code, when offering Google, then its link carries the invitation", async () => {
    vi.mocked(authService.verifyInvitation).mockResolvedValue(undefined);
    renderActivate("/activate?email=rui%40example.com&token=abc123");

    await enterCode("ABC123");

    const link = await screen.findByRole("link", {
      name: "Continue with Google",
    });
    const url = new URL(link.getAttribute("href")!);
    expect(url.pathname).toBe("/auth/external/google");
    expect(Object.fromEntries(url.searchParams)).toEqual({
      intent: "activate",
      email: "rui@example.com",
      token: "abc123",
      code: "ABC123",
    });
  });

  it("given pending Google details, when the page opens, then the code is skipped and the member activates with editable names", async () => {
    vi.mocked(authService.pendingExternal).mockResolvedValue({
      intent: "activate",
      email: "rui@example.com",
      firstName: "Rui",
      lastName: "Costa",
      organizationName: null,
    });
    vi.mocked(authService.activateExternal).mockResolvedValue(undefined);
    renderActivate(
      "/activate?email=rui%40example.com&token=abc123&external=google",
    );
    const user = userEvent.setup();

    const lastName = await screen.findByLabelText("Last name");
    expect(screen.getByLabelText("First name")).toHaveValue("Rui");
    expect(
      screen.queryByLabelText("Invitation code 1/6"),
    ).not.toBeInTheDocument();
    expect(screen.queryByLabelText("Password")).not.toBeInTheDocument();
    await user.clear(lastName);
    await user.type(lastName, "Costa Silva");
    await user.click(screen.getByRole("button", { name: "Create account" }));

    expect(authService.activateExternal).toHaveBeenCalledWith({
      firstName: "Rui",
      lastName: "Costa Silva",
    });
    expect(await screen.findByText("Home")).toBeInTheDocument();
  });

  it("given the Google account is not the invited one, when activating, then it says to use the invited account", async () => {
    vi.mocked(authService.pendingExternal).mockResolvedValue({
      intent: "activate",
      email: "other@example.com",
      firstName: "Eva",
      lastName: "Other",
      organizationName: null,
    });
    vi.mocked(authService.activateExternal).mockRejectedValue({
      response: { status: 403 },
    });
    renderActivate(
      "/activate?email=rui%40example.com&token=abc123&external=google",
    );
    const user = userEvent.setup();

    await screen.findByLabelText("First name");
    await user.click(screen.getByRole("button", { name: "Create account" }));

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Use the Google account of the invited email.",
    );
  });
});
