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
      "invitation is not valid",
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
    expect(
      await screen.findByText(/new code was just sent/),
    ).toBeInTheDocument();
  });
});
