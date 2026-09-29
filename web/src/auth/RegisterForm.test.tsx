import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AuthProvider } from "./AuthContext";
import { LoginPage } from "./LoginPage";
import { RegisterPage } from "./RegisterPage";
import { authService } from "./authService";

vi.mock("./authService");

function renderRegister() {
  return render(
    <MemoryRouter initialEntries={["/register"]}>
      <AuthProvider>
        <Routes>
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/login" element={<LoginPage />} />
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
});
