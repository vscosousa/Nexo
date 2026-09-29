import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AuthProvider } from "./AuthContext";
import { LoginPage } from "./LoginPage";
import { RegisterPage } from "./RegisterPage";
import { authService } from "./authService";

vi.mock("./authService");

function renderRegister(url = "/register/organization?planId=team-id") {
  return render(
    <MemoryRouter initialEntries={[url]}>
      <AuthProvider>
        <Routes>
          <Route path="/register/organization" element={<RegisterPage />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/app" element={<p>Home</p>} />
        </Routes>
      </AuthProvider>
    </MemoryRouter>,
  );
}

async function fillStep1(organizationName: string) {
  const user = userEvent.setup();
  if (organizationName) {
    await user.type(
      screen.getByLabelText("Organization name"),
      organizationName,
    );
  }
  await user.click(screen.getByRole("button", { name: "Continue" }));
}

async function fillStep2(values: Record<string, string>) {
  const user = userEvent.setup();
  for (const [label, value] of Object.entries(values)) {
    if (value) await user.type(screen.getByLabelText(label), value);
  }
  await user.click(
    screen.getByRole("button", { name: "Register organization" }),
  );
}

const valid = {
  "First name": "Ana",
  "Last name": "Admin",
  Email: "ana@example.com",
  Password: "Secret-123",
};

describe("RegisterForm", () => {
  beforeEach(() => vi.resetAllMocks());

  it("given valid details across both steps, when submitting, then it registers and sends the admin to sign in", async () => {
    vi.mocked(authService.register).mockResolvedValue(undefined);
    renderRegister();

    await fillStep1("Associação Horizonte");
    await fillStep2(valid);

    expect(authService.register).toHaveBeenCalledWith({
      organizationName: "Associação Horizonte",
      adminFirstName: "Ana",
      adminLastName: "Admin",
      adminEmail: "ana@example.com",
      password: "Secret-123",
      planId: "team-id",
    });
    expect(await screen.findByRole("status")).toHaveTextContent(
      "Organization registered",
    );
  });

  it("given an empty organization name, when continuing, then it asks for it and stays on step one", async () => {
    renderRegister();

    await fillStep1("");

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Fill in every field.",
    );
    expect(
      screen.getByRole("button", { name: "Continue" }),
    ).toBeInTheDocument();
  });

  it("given step two, when going back, then step one keeps the organization name", async () => {
    const user = userEvent.setup();
    renderRegister();

    await fillStep1("Associação Horizonte");
    await user.click(
      screen.getByRole("button", { name: "Back: Organization" }),
    );

    expect(screen.getByLabelText("Organization name")).toHaveValue(
      "Associação Horizonte",
    );
  });

  it("given an empty field on step two, when submitting, then it asks for it without calling the API", async () => {
    renderRegister();

    await fillStep1("Associação Horizonte");
    await fillStep2({ ...valid, Password: "" });

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Fill in every field.",
    );
    expect(authService.register).not.toHaveBeenCalled();
  });

  it("given step two, when typing the password, then the requirements checklist updates live", async () => {
    const user = userEvent.setup();
    renderRegister();

    await fillStep1("Associação Horizonte");
    const requirement = () => screen.getByText("At least 8 characters");
    expect(requirement().closest("li")).not.toHaveAttribute("data-met");

    await user.type(screen.getByLabelText("Password"), "Secret-1");

    expect(requirement().closest("li")).toHaveAttribute("data-met");
  });

  it("given a malformed email on step two, when submitting, then it asks for a valid one without calling the API", async () => {
    renderRegister();

    await fillStep1("Associação Horizonte");
    await fillStep2({ ...valid, Email: "not-an-email" });

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Enter a valid email.",
    );
    expect(authService.register).not.toHaveBeenCalled();
  });

  it("given a password missing a symbol on step two, when submitting, then it asks for a stronger one without calling the API", async () => {
    renderRegister();

    await fillStep1("Associação Horizonte");
    await fillStep2({ ...valid, Password: "Secret123" });

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "at least 8 characters",
    );
    expect(authService.register).not.toHaveBeenCalled();
  });

  it("given the email already registered, when submitting, then it says so", async () => {
    vi.mocked(authService.register).mockRejectedValue({
      response: { status: 409 },
    });
    renderRegister();

    await fillStep1("Associação Horizonte");
    await fillStep2(valid);

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "already registered",
    );
  });

  it("given the API rejects a field, when submitting, then it shows the API's message", async () => {
    vi.mocked(authService.register).mockRejectedValue({
      response: {
        status: 400,
        data: { errors: { Password: ["Password is too short."] } },
      },
    });
    renderRegister();

    await fillStep1("Associação Horizonte");
    await fillStep2(valid);

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Password is too short.",
    );
  });

  it("given step two, when offering Google, then its link carries the plan and organization name", async () => {
    renderRegister();

    await fillStep1("Local Club");

    const link = screen.getByRole("link", { name: "Continue with Google" });
    const url = new URL(link.getAttribute("href")!);
    expect(url.pathname).toBe("/auth/external/google");
    expect(Object.fromEntries(url.searchParams)).toEqual({
      intent: "register",
      planId: "team-id",
      organizationName: "Local Club",
    });
  });

  it("given pending Google details, when the form opens, then the names are prefilled and editable and no password is asked", async () => {
    vi.mocked(authService.pendingExternal).mockResolvedValue({
      intent: "register",
      email: "ana@example.com",
      firstName: "Ana",
      lastName: "Silva",
      organizationName: "Local Club",
    });
    vi.mocked(authService.registerExternal).mockResolvedValue(undefined);
    renderRegister("/register/organization?planId=team-id&external=google");
    const user = userEvent.setup();

    expect(await screen.findByText(/ana@example\.com/)).toBeInTheDocument();
    const firstName = screen.getByLabelText("First name");
    expect(firstName).toHaveValue("Ana");
    expect(screen.getByLabelText("Last name")).toHaveValue("Silva");
    expect(screen.queryByLabelText("Password")).not.toBeInTheDocument();
    await user.clear(firstName);
    await user.type(firstName, "Anabela");
    await user.click(
      screen.getByRole("button", { name: "Register organization" }),
    );

    expect(authService.registerExternal).toHaveBeenCalledWith({
      organizationName: "Local Club",
      adminFirstName: "Anabela",
      adminLastName: "Silva",
      planId: "team-id",
    });
    expect(await screen.findByText("Home")).toBeInTheDocument();
  });

  it("given Google sign-in failed, when the form opens, then it says so", async () => {
    renderRegister("/register/organization?planId=team-id&error=oauth");

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Google sign-in failed.",
    );
  });

  it("given the Google details expired, when the form opens, then it asks to try again", async () => {
    vi.mocked(authService.pendingExternal).mockRejectedValue({
      response: { status: 401 },
    });
    renderRegister("/register/organization?planId=team-id&external=google");

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Your Google sign-in expired",
    );
  });
});
