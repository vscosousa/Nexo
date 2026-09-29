import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AuthProvider } from "./AuthContext";
import { LoginPage } from "./LoginPage";
import { authService } from "./authService";

vi.mock("./authService");

function renderLogin(entry = "/login") {
  return render(
    <MemoryRouter initialEntries={[entry]}>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/app" element={<p>Home</p>} />
        </Routes>
      </AuthProvider>
    </MemoryRouter>,
  );
}

async function submit(email: string, password: string) {
  const user = userEvent.setup();
  if (email) await user.type(screen.getByLabelText("Email"), email);
  if (password) await user.type(screen.getByLabelText("Password"), password);
  await user.click(screen.getByRole("button", { name: "Sign in" }));
}

describe("SignInForm", () => {
  beforeEach(() => {
    localStorage.clear();
    vi.resetAllMocks();
  });

  it("given correct credentials, when submitting, then it opens the app without keeping any token in script-readable storage", async () => {
    vi.mocked(authService.signIn).mockResolvedValue(undefined);
    renderLogin();

    await submit("ana@example.com", "secret");

    expect(authService.signIn).toHaveBeenCalledWith(
      "ana@example.com",
      "secret",
    );
    expect(await screen.findByText("Home")).toBeInTheDocument();
    expect(localStorage.length).toBe(0);
  });

  it("given the API rejects the credentials, when submitting, then it shows one generic error and stays on the form", async () => {
    vi.mocked(authService.signIn).mockRejectedValue({
      response: { status: 401 },
    });
    renderLogin();

    await submit("ana@example.com", "wrong");

    const alert = await screen.findByRole("alert");
    expect(alert).toHaveTextContent("Wrong email or password.");
    expect(screen.queryByText("Home")).not.toBeInTheDocument();
  });

  it("given too many attempts, when submitting, then it says how long to wait instead of asking to try again", async () => {
    vi.mocked(authService.signIn).mockRejectedValue({
      response: { status: 429, headers: { "retry-after": "37" } },
    });
    renderLogin();

    await submit("ana@example.com", "secret");

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Too many attempts. Wait up to 37 s.",
    );
  });

  it("given the server cannot be reached, when submitting, then it says so", async () => {
    vi.mocked(authService.signIn).mockRejectedValue(new Error("Network Error"));
    renderLogin();

    await submit("ana@example.com", "secret");

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Can't reach Nexo. Check your connection.",
    );
  });

  it("given an empty field, when submitting, then it asks for it without calling the API", async () => {
    renderLogin();

    await submit("ana@example.com", "");

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Enter your email and password.",
    );
    expect(authService.signIn).not.toHaveBeenCalled();
  });

  it("given a malformed email, when submitting, then it asks for a valid one without calling the API", async () => {
    renderLogin();

    await submit("not-an-email", "secret");

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Enter a valid email.",
    );
    expect(authService.signIn).not.toHaveBeenCalled();
  });

  it("given the page opened with a success notice, when a sign-in fails, then only the error remains", async () => {
    vi.mocked(authService.signIn).mockRejectedValue({
      response: { status: 401 },
    });
    renderLogin("/login?registered=1");
    expect(screen.getByRole("status")).toBeInTheDocument();

    await submit("ana@example.com", "wrong");

    expect(await screen.findByRole("alert")).toBeInTheDocument();
    expect(screen.queryByRole("status")).not.toBeInTheDocument();
  });

  it("given a failed Google sign-in redirect, when the page opens, then it shows the generic error", () => {
    renderLogin("/login?error=oauth");

    expect(screen.getByRole("alert")).toHaveTextContent(
      "Google sign-in failed.",
    );
  });

  it("when the page opens, then Google sign-in starts at the API’s public address, not the dev proxy", () => {
    renderLogin();

    expect(
      screen.getByRole("link", { name: "Continue with Google" }),
    ).toHaveAttribute("href", "http://localhost:5122/auth/external/google");
  });

  it("when the show-password button is pressed, then the password is revealed and can be hidden again", async () => {
    renderLogin();
    const password = screen.getByLabelText("Password");
    expect(password).toHaveAttribute("type", "password");

    await userEvent.click(
      screen.getByRole("button", { name: "Show password" }),
    );
    expect(password).toHaveAttribute("type", "text");

    await userEvent.click(
      screen.getByRole("button", { name: "Hide password" }),
    );
    expect(password).toHaveAttribute("type", "password");
  });
});
